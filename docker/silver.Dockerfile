# Spark job 2: S3 bronze/ -> silver/ (Delta), dlq/ and export/.
FROM apache/spark:3.5.5-scala2.12-java17-python3-ubuntu

USER root
WORKDIR /app

# delta-spark Python helper (--no-deps keeps the Spark that ships with the image);
# python-dotenv because the job can read settings from a .env file.
RUN pip3 install --no-cache-dir --no-deps delta-spark==3.2.0 \
 && pip3 install --no-cache-dir python-dotenv importlib-metadata

COPY spark/silver_processing.py /app/spark/silver_processing.py

RUN mkdir -p /tmp/spark-ivy

ENV HOME=/root \
    SPARK_LOCAL_DIRS=/tmp \
    PYSPARK_PYTHON=python3
