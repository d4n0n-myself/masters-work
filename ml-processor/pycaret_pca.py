import numpy as np
import pandas as pd
from sklearn.datasets import load_iris
from sklearn.decomposition import PCA
from pycaret.classification import *
from sklearn.model_selection import train_test_split

# Загрузка датасета Iris
iris = load_iris()
# Разделение данных на обучающий и тестовый наборы
X_train, X_test, y_train, y_test = train_test_split(iris.data, iris.target, train_size=0.75, test_size=0.25)


# Применение PCA
pca = PCA(n_components=2)
X_pca = pca.fit_transform(X_train)

# Создание нового датасета с использованием новых признаков
new_data = np.concatenate((X_pca, y_train.reshape(-1, 1)), axis=1)
new_data = pd.DataFrame(new_data, columns=['feature1', 'feature2', 'target'])

# Создание и обучение модели с помощью PyCaret
clf = setup(data=new_data, target='target')
best_model = compare_models()

# Прогнозирование на новых данных
new_X_pca = pca.transform(X_test)
new_data = pd.DataFrame(np.concatenate((new_X_pca, y_test.reshape(-1, 1)), axis=1),
                        columns=['feature1', 'feature2', 'target'])
predictions = predict_model(best_model, data=new_data)

print(predictions)
