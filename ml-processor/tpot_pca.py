import numpy as np
import pandas as pd
from sklearn.datasets import load_iris
from sklearn.decomposition import PCA
from sklearn.model_selection import train_test_split
from tpot import TPOTClassifier

# Загрузка датасета Iris
iris = load_iris()
X_train, X_test, y_train, y_test = train_test_split(iris.data, iris.target, train_size=0.75, test_size=0.25)

# Применение PCA
pca = PCA(n_components=2)
X_pca = pca.fit_transform(X_train)

# Создание нового датасета с использованием новых признаков
new_data = np.concatenate((X_pca, y_train.reshape(-1, 1)), axis=1)
new_data = pd.DataFrame(new_data, columns=['feature1', 'feature2', 'target'])

# Создание и обучение модели с помощью TPOT
tpot = TPOTClassifier(generations=5, population_size=20, verbosity=2)
tpot.fit(X_train, y_train)

# Прогнозирование на новых данных
predictions = tpot.score(X_test, y_test)

print("fitted pipelines")
print(tpot.pareto_front_fitted_pipelines_)

print("models:")
# Вывод использованных моделей
for model in tpot.fitted_pipeline_:
    print(model)

print("predictions")
print(predictions)
