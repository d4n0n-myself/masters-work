import numpy as np
import pandas as pd
from sklearn.datasets import load_iris
from sklearn.decomposition import PCA
from sklearn.model_selection import train_test_split
from autogluon.tabular import TabularPredictor

# Загрузка датасета Iris
iris = load_iris()
# Разделение данных на обучающий и тестовый наборы
X_train, X_test, y_train, y_test = train_test_split(iris.data, iris.target, train_size=0.75, test_size=0.25)

# Применение PCA на обучающем наборе
pca = PCA(n_components=2)
X_train_pca = pca.fit_transform(X_train)

# Создание нового датасета с использованием новых признаков
new_data = np.concatenate((X_train_pca, y_train.reshape(-1, 1)), axis=1)
new_data = pd.DataFrame(new_data, columns=['feature1', 'feature2', 'target'])

# Создание и обучение модели с помощью AutoGluon
predictor = TabularPredictor(label='target').fit(new_data, presets='medium_quality')

# Прогнозирование на новых данных
X_test_pca = pca.transform(X_test)
new_data_test = pd.DataFrame(np.concatenate((X_test_pca, y_test.reshape(-1, 1)), axis=1),
                             columns=['feature1', 'feature2', 'target'])
predictions = predictor.predict(new_data_test)

print(predictions)
