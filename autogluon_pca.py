import numpy as np
import pandas as pd
from sklearn.datasets import load_iris
from sklearn.decomposition import PCA
from autogluon.tabular import TabularPredictor

# Загрузка датасета Iris
iris = load_iris()
X = iris.data
y = iris.target

# Применение PCA
pca = PCA(n_components=2)
X_pca = pca.fit_transform(X)

# Создание нового датасета с использованием новых признаков
new_data = np.concatenate((X_pca, y.reshape(-1, 1)), axis=1)
new_data = pd.DataFrame(new_data, columns=['feature1', 'feature2', 'target'])

# Создание и обучение модели с помощью AutoGluon
predictor = TabularPredictor(label='target').fit(new_data, presets='medium_quality')
# predictor = TabularPredictor.load("AutogluonModels/ag-20240209_065058")

# Прогнозирование на новых данных
new_X = np.array([[5.1, 3.5, 1.4, 0.2], [5.9, 3.0, 5.1, 1.8], [5.1, 2.5, 3.0, 1.1]])  # Пример новых данных
new_X_pca = pca.transform(new_X)
new_data = pd.DataFrame(np.concatenate((new_X_pca, [[0], [2], [1]]), axis=1),
                        columns=['feature1', 'feature2', 'target'])
predictions = predictor.predict(new_data)

print(predictions)
