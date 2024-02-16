import os
from kafka import KafkaConsumer, KafkaProducer
from minio import Minio
import pandas as pd
import autogluon as ag
import autokeras as ak
from tpot import TPOTClassifier
from pycaret.classification import *

# Получение настроек Kafka из переменных среды
kafka_bootstrap_servers = os.environ.get('KAFKA_BOOTSTRAP_SERVERS')
datasets_input_topic = os.environ.get('DATASETS_INPUT_TOPIC')
datasets_output_topic = os.environ.get('DATASETS_OUTPUT_TOPIC')

# Получение настроек Minio из переменных среды
minio_endpoint = os.environ.get('MINIO_ENDPOINT')
minio_access_key = os.environ.get('MINIO_ACCESS_KEY')
minio_secret_key = os.environ.get('MINIO_SECRET_KEY')
minio_bucket_name = os.environ.get('MINIO_BUCKET_NAME')

# Создание Kafka consumer и producer
consumer = KafkaConsumer(datasets_input_topic, bootstrap_servers=kafka_bootstrap_servers)
producer = KafkaProducer(bootstrap_servers=kafka_bootstrap_servers)

# Создание Minio клиента
minio_client = Minio(minio_endpoint, access_key=minio_access_key, secret_key=minio_secret_key)


def process_message_with_autogluon(df, dataset_id):
    # Разделение на тренировочный и тестовый датасет
    train_data, test_data = ag.utils.split_dataframe(df, ratio=0.8)

    # Метод классификации с AutoGluon
    predictor = ag.TabularPredictor(label='target').fit(train_data)

    # Классификация тестовых значений
    predictions = predictor.predict(test_data)

    # Создание CSV файла с результатами классификации
    predictions.to_csv(f'autogluon_predictions.csv', index=False)

    # Загрузка CSV файла с результатами в Minio
    minio_client.fput_object(minio_bucket_name, f'{dataset_id}_predictions.csv', f'{dataset_id}_predictions.csv')

    # Создание сообщения в топик datasets_output
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=f'{dataset_id}_predictions.csv'.encode())


def process_message_with_autokeras(df, dataset_id):
    # Использование AutoKeras
    clf = ak.StructuredDataClassifier(max_trials=10)
    clf.fit(train_data, train_data['target'])
    ak_predictions = clf.predict(test_data)

    # Запись результатов классификации AutoKeras в CSV файл
    ak_predictions.to_csv(f'{dataset_id}_ak_predictions.csv', index=False)
    minio_client.fput_object(minio_bucket_name, f'{dataset_id}_ak_predictions.csv', f'{dataset_id}_ak_predictions.csv')
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=f'{dataset_id}_ak_predictions.csv'.encode())


def process_message_with_tpot(df, dataset_id):
    # Использование TPOT
    tpot_clf = TPOTClassifier(generations=5, population_size=20, verbosity=2)
    tpot_clf.fit(train_data.drop('target', axis=1), train_data['target'])
    tpot_predictions = tpot_clf.predict(test_data.drop('target', axis=1))

    # Запись результатов классификации TPOT в CSV файл
    tpot_predictions.to_csv(f'{dataset_id}_tpot_predictions.csv', index=False)
    minio_client.fput_object(minio_bucket_name, f'{dataset_id}_tpot_predictions.csv', f'{dataset_id}_tpot_predictions.csv')
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=f'{dataset_id}_tpot_predictions.csv'.encode())


def process_message_with_pycaret(df, dataset_id):
    # Использование PyCaret
    setup(data=train_data, target='target')
    pycaret_clf = compare_models()
    pycaret_predictions = predict_model(pycaret_clf, data=test_data)

    # Запись результатов классификации PyCaret в CSV файл
    pycaret_predictions.to_csv(f'{dataset_id}_pycaret_predictions.csv', index=False)
    minio_client.fput_object(minio_bucket_name, f'{dataset_id}_pycaret_predictions.csv', f'{dataset_id}_pycaret_predictions.csv')
    producer.send(datasets_output_topic, key=dataset_id.encode(), value=f'{dataset_id}_pycaret_predictions.csv'.encode())


# Потребление сообщений из топика datasets_input
for message in consumer:
    # Получение сообщения
    dataset_id = message.key.decode()
    dataset_path = message.value.decode()

    # Скачивание CSV файла из Minio
    minio_client.fget_object(minio_bucket_name, dataset_path, f'{dataset_id}.csv')
    csv_file_path = f'{dataset_id}.csv'

    # Загрузка CSV файла в DataFrame
    df = pd.read_csv(csv_file_path) 
    process_message_with_autogluon(df, dataset_id)
    process_message_with_autokeras(df, dataset_id)
    process_message_with_tpot(df, dataset_id)
    process_message_with_pycaret(df, dataset_id)


