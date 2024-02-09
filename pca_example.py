import numpy as np
import matplotlib.pyplot as plt
from sklearn import decomposition
from sklearn import datasets

# Загрузка данных Iris
iris = datasets.load_iris()
X = iris.data
y = iris.target

# Применение PCA
pca = decomposition.PCA(n_components=2)  # Указываем количество компонент
X_pca = pca.fit_transform(X)

# Визуализация результатов
plt.scatter(X_pca[:, 0], X_pca[:, 1], c=y, cmap=plt.cm.Set1)
plt.xlabel('Principal Component 1')
plt.ylabel('Principal Component 2')
plt.title('PCA on Iris dataset')
plt.show()
