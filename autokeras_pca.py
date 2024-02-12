import numpy as np
import pandas as pd
import autokeras as ak
from sklearn.datasets import load_iris
from sklearn.decomposition import PCA
from sklearn.model_selection import train_test_split

# Загрузка датасета Iris
iris = load_iris()
# Разделение данных на обучающий и тестовый наборы
x_train, X_test, y_train, y_test = train_test_split(iris.data, iris.target, train_size=0.75, test_size=0.25)

# Применение PCA на обучающем наборе
pca = PCA(n_components=2)
X_train_pca = pca.fit_transform(x_train)

# Создание нового датасета с использованием новых признаков
new_data = np.concatenate((X_train_pca, y_train.reshape(-1, 1)), axis=1)
new_data = pd.DataFrame(new_data, columns=['feature1', 'feature2', 'target'])

# Initialize the structured data classifier.
clf = ak.StructuredDataClassifier(
    overwrite=True, max_trials=10
)  # It tries 10 different models.
# Feed the structured data classifier with training data.
clf.fit(
    X_train_pca,
    y_train,
    epochs=50,
)

# Прогнозирование на новых данных
X_test_pca = pca.transform(X_test)
predict_res = clf.predict(X_test_pca)
print(predict_res)

# Evaluate the best model with testing data.
eval_res = clf.evaluate(X_test_pca, y_test)
print(eval_res)

model = clf.export_model()
model.summary()