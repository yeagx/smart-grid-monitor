"""Spark job 1: Kafka -> S3 bronze/.

Subscribes to the meter.readings and transformer.readings topics and saves every
message exactly as it arrived (raw JSON text + Kafka topic, partition, offset and
time) as Parquet in bronze/meter_readings/ and bronze/transformer_readings/.
Nothing is cleaned here: Bronze is the untouched copy we can always go back to.
A checkpoint remembers the last Kafka offset, so a restart continues where it stopped.
"""
import os

from pyspark.sql import SparkSession
from pyspark.sql.functions import col


KAFKA_BOOTSTRAP_SERVERS = os.getenv(
    "KAFKA_BOOTSTRAP_SERVERS", "kafka:29092"
)

KAFKA_TOPICS = [
    topic.strip()
    for topic in os.getenv(
        "KAFKA_TOPICS",
        "meter.readings,transformer.readings"
    ).split(",")
    if topic.strip()
]

STARTING_OFFSETS = os.getenv("KAFKA_STARTING_OFFSETS", "latest")

OUTPUT_BASE_PATH = os.getenv(
    "BRONZE_OUTPUT_PATH", "/app/bronze"
).rstrip("/")

CHECKPOINT_ROOT = os.getenv(
    "SPARK_CHECKPOINT_PATH", "/app/checkpoints/kafka_raw"
).rstrip("/")

TRIGGER_INTERVAL = os.getenv(
    "SPARK_TRIGGER_INTERVAL", "60 seconds"
)


TOPIC_GROUPS = {
    "meter_readings": [
        topic for topic in KAFKA_TOPICS
        if topic.startswith("meter.readings")
    ],
    "transformer_readings": [
        topic for topic in KAFKA_TOPICS
        if topic.startswith("transformer.readings")
    ],
}

UNKNOWN_TOPICS = [
    topic for topic in KAFKA_TOPICS
    if not (
        topic.startswith("meter.readings")
        or topic.startswith("transformer.readings")
    )
]

if not KAFKA_TOPICS:
    raise ValueError("KAFKA_TOPICS must contain at least one topic.")

if UNKNOWN_TOPICS:
    raise ValueError(
        f"Unsupported topics in KAFKA_TOPICS: {UNKNOWN_TOPICS}"
    )

ACTIVE_GROUPS = {
    name: topics
    for name, topics in TOPIC_GROUPS.items()
    if topics
}

if not ACTIVE_GROUPS:
    raise ValueError(
        "No meter.readings or transformer.readings topics were configured."
    )


def join_path(base, child):
    return f"{base.rstrip('/')}/{child}"


spark = (
    SparkSession.builder
    .appName("SmartGridRawKafkaToBronzeParquet")
    .getOrCreate()
)

spark.sparkContext.setLogLevel("WARN")

queries = []

try:
    for group_name, topics in ACTIVE_GROUPS.items():
        source = (
            spark.readStream
            .format("kafka")
            .option("kafka.bootstrap.servers", KAFKA_BOOTSTRAP_SERVERS)
            .option("subscribe", ",".join(topics))
            .option("startingOffsets", STARTING_OFFSETS)
            .load()
        )

        bronze = source.select(
            col("topic").cast("string").alias("topic"),
            col("partition").cast("integer").alias("partition"),
            col("offset").cast("long").alias("offset"),
            col("timestamp").alias("kafka_timestamp"),
            col("timestampType").cast("integer").alias(
                "kafka_timestamp_type"
            ),
            col("key").cast("string").alias("message_key"),
            col("value").cast("string").alias("raw_value"),  # Spark job 2 reads this column name
        )

        output_path = join_path(OUTPUT_BASE_PATH, group_name)
        checkpoint_path = join_path(CHECKPOINT_ROOT, group_name)

        query = (
            bronze.writeStream
            .format("parquet")
            .outputMode("append")
            .option("path", output_path)
            .option("checkpointLocation", checkpoint_path)
            .trigger(processingTime=TRIGGER_INTERVAL)
            .start()
        )

        queries.append(query)

        print(
            f"Topics: {','.join(topics)}\n"
            f"Parquet output: {output_path}\n"
            f"Checkpoint: {checkpoint_path}\n",
            flush=True,
        )

    spark.streams.awaitAnyTermination()

finally:
    for query in queries:
        if query.isActive:
            query.stop()

    spark.stop()
