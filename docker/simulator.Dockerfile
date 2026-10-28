# Simulator image: the data generator, the output validator and the Kafka producer.
FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY requirements.txt ./requirements.txt
RUN python -m pip install --no-cache-dir --upgrade pip \
    && python -m pip install --no-cache-dir -r requirements.txt

COPY config.yaml ./config.yaml
COPY simulator ./simulator

RUN mkdir -p /app/output/grid

# Default action: generate the data and validate it (no Kafka publishing).
CMD ["sh", "-c", "python simulator/generator.py --config config.yaml --out /app/output && python simulator/validate_outputs.py /app/output"]
