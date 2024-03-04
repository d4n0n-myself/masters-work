# Use the official Python 3.9 base image
FROM python:3.9

# Set the working directory in the container
WORKDIR .

# Copy the Python file to the working directory
COPY /ml-processor/ml-processor.py .
COPY /ml-processor/requirements.txt .

RUN pip install --upgrade pip
RUN pip install -U setuptools wheel
RUN pip install -r requirements.txt

# Run the Python file when the container starts
CMD ["python", "ml-processor.py"]
