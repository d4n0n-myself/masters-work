import json
import os
from kafka import KafkaConsumer, KafkaProducer
from minio import Minio
import pandas as pd
import autogluon.tabular as ag
import autokeras as ak
from sklearn.model_selection import train_test_split
from tpot import TPOTClassifier
from pycaret.classification import *

class Empty:
    pass

# Получение настроек Kafka из переменных среды
kafka_bootstrap_servers = os.environ.get('KAFKA_BOOTSTRAP_SERVERS')
datasets_input_topic = 'datasets_input'
datasets_output_topic = 'datasets_output'

# Получение настроек Minio из переменных среды
minio_endpoint = os.environ.get('MINIO_ENDPOINT')
minio_access_key = os.environ.get('MINIO_ACCESS_KEY')
minio_secret_key = os.environ.get('MINIO_SECRET_KEY')
minio_input_bucket_name = 'datasets-input'
minio_output_bucket_name = 'datasets-output'

# Создание Kafka consumer и producer
consumer = KafkaConsumer(datasets_input_topic, group_id="python-client", bootstrap_servers=kafka_bootstrap_servers)
producer = KafkaProducer(bootstrap_servers=kafka_bootstrap_servers)

# Создание Minio клиента
minio_client = Minio(minio_endpoint, access_key=minio_access_key, secret_key=minio_secret_key, secure=False)


def process_message_with_autogluon(df, dataset_id):
    # Разделение на тренировочный и тестовый датасет
    train_data, test_data = train_test_split(df, train_size=df.shape[0]-25, test_size=25)

    # Метод классификации с AutoGluon
    predictor = ag.TabularPredictor(label='output').fit(train_data)

    # Классификация тестовых значений
    predictions = predictor.predict(test_data)

    accuracy_score = predictor.evaluate(test_data, silent=True)
    test_data.insert(df.columns.size, 'prediction', predictions)
    file_name = f'{dataset_id}_autogluon_predictions.csv'
    # Создание CSV файла с результатами классификации
    test_data.to_csv(file_name, index=False)

    # Загрузка CSV файла с результатами в Minio
    minio_client.fput_object(minio_output_bucket_name, file_name, file_name, content_type='text/csv')

    obj = Empty()
    obj.file_name = file_name
    obj.accuracy = accuracy_score['accuracy']
    obj.best_model = predictor.model_best

    # Создание сообщения в топик datasets_output
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=json.dumps(obj.__dict__).encode())


def process_message_with_autokeras(df, dataset_id):
    train_data, test_data = train_test_split(df, train_size=df.shape[0]-25, test_size=25)
    # Использование AutoKeras
    clf = ak.StructuredDataClassifier(max_trials=10)
    fit_result = clf.fit(train_data, train_data['output'])
    predictions = clf.predict(test_data)
    loss, accuracy = clf.evaluate(test_data, test_data['output'])

    predictions = [int(iterator) for iterator in predictions]
    test_data.insert(df.columns.size, 'prediction', predictions)
    file_name = f'{dataset_id}_autokeras_predictions.csv'
    test_data.to_csv(file_name, index=False)
    minio_client.fput_object(minio_output_bucket_name, file_name, file_name, content_type='text/csv')

    obj = Empty()
    obj.file_name = file_name
    obj.accuracy = round(accuracy, 2)
    obj.best_model = fit_result.model.output_names[0]
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=json.dumps(obj.__dict__).encode())


def process_message_with_tpot(df, dataset_id):
    train_data, test_data = train_test_split(df, train_size=df.shape[0]-25, test_size=25)
    # Использование TPOT
    tpot_clf = TPOTClassifier(generations=5, population_size=20, verbosity=2)
    tpot_clf.fit(train_data.drop('output', axis=1), train_data['output'])
    predictions = tpot_clf.predict(test_data.drop('output', axis=1))
    accuracy = tpot_clf.score(train_data.drop('output', axis=1), train_data['output'])
    my_dict = list(tpot_clf.evaluated_individuals_.items())

    model_scores = pd.DataFrame(columns=['model', 'cv_score', 'model_info'])
    for model in my_dict:
        model_name = model[0]
        model_info = model[1]
        cv_score = model[1].get('internal_cv_score')  # Pull out cv_score as a column (i.e., sortable)
        model_scores.loc[len(model_scores)] = {'model': model_name,
                                        'cv_score': cv_score,
                                        'model_info': model_info,}

    model_scores = model_scores.sort_values('cv_score', ascending=False)
    best_model = model_scores['model'][0]

    test_data.insert(df.columns.size, 'prediction', predictions)
    file_name = f'{dataset_id}_tpot_predictions.csv'
    test_data.to_csv(file_name, index=False)
    minio_client.fput_object(minio_output_bucket_name, file_name, file_name, content_type='text/csv')

    obj = Empty()
    obj.file_name = file_name
    obj.accuracy = round(accuracy, 2)
    obj.best_model = best_model
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=json.dumps(obj.__dict__).encode())


def process_message_with_pycaret(df, dataset_id):
    train_data, test_data = train_test_split(df, train_size=df.shape[0]-25, test_size=25)
    # Использование PyCaret
    setup(data=train_data, target='output')
    pycaret_clf = compare_models()
    predictions = predict_model(pycaret_clf, data=test_data)
    results = pull()
    accuracy = results['Accuracy'][0]
    best_model = results['Model'][0]

    test_data.insert(df.columns.size, 'prediction', predictions['prediction_label'])
    file_name = f'{dataset_id}_pycaret_predictions.csv'
    # Запись результатов классификации PyCaret в CSV файл
    test_data.to_csv(file_name, index=False)
    minio_client.fput_object(minio_output_bucket_name, file_name, file_name, content_type='text/csv')

    obj = Empty()
    obj.file_name = file_name
    obj.accuracy = round(accuracy, 2)
    obj.best_model = best_model
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=json.dumps(obj.__dict__).encode())


# Потребление сообщений из топика datasets_input
for message in consumer:
    # Получение сообщения
    trace_id = message.key.decode()
    dataset_path = message.value.decode()
    csv_file_path = f'{trace_id}.csv'

    # Скачивание CSV файла из Minio
    minio_client.fget_object(minio_input_bucket_name, dataset_path, csv_file_path)

    # Загрузка CSV файла в DataFrame
    dataframe = pd.read_csv(csv_file_path)
    process_message_with_autogluon(dataframe, trace_id)
    process_message_with_autokeras(dataframe, trace_id)
    process_message_with_tpot(dataframe, trace_id)
    process_message_with_pycaret(dataframe, trace_id)

    if os.path.exists(csv_file_path) and os.path.isfile(csv_file_path):
        os.remove(csv_file_path)

    processes = ['autogluon', 'autokeras', 'tpot', 'pycaret']

    for i in range(4):
        file_path = f'{trace_id}_{processes[i]}_predictions.csv'
        # Check if the file exists
        if os.path.exists(file_path) and os.path.isfile(file_path):
            # Remove the file
            os.remove(file_path)

    print('done processing message')

