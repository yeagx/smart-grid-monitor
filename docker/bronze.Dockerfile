# Spark job 1: Kafka -> S3 bronze/ (raw messages, Parquet).
FROM apache/spark:3.5.5

USER root
WORKDIR /app

COPY spark/bronze_ingest.py /app/spark/bronze_ingest.py

RUN mkdir -p /app/bronze /app/checkpoints /tmp/spark-ivy /home/spark \
    && chown -R 1000:1000 /app /tmp/spark-ivy /home/spark

ENV HOME=/root \
    USER=root \
    LOGNAME=root \
    SPARK_LOCAL_DIRS=/tmp \
    PYSPARK_PYTHON=python3

CMD ["/opt/spark/bin/spark-submit", "--master", "local[2]", "--conf", "spark.jars.ivy=/tmp/spark-ivy", "--packages", "org.apache.spark:spark-sql-kafka-0-10_2.12:3.5.5,org.apache.hadoop:hadoop-aws:3.3.4", "/app/spark/bronze_ingest.py"]
