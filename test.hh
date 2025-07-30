version: '3.8'

services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.4.0
    hostname: zookeeper
    container_name: zookeeper
    ports:
      - "2181:2181"
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000

  kafka:
    image: confluentinc/cp-kafka:7.4.0
    hostname: kafka
    container_name: kafka
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
      - "9101:9101"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: 'zookeeper:2181'
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
      KAFKA_JMX_PORT: 9101
      KAFKA_JMX_HOSTNAME: localhost

  # Service to create topics automatically
  kafka-topics-generator:
    image: confluentinc/cp-kafka:7.4.0
    container_name: kafka-topics-generator
    depends_on:
      - kafka
    command: >
      bash -c
        "sleep 10s &&
        kafka-topics --create --topic=user-events --if-not-exists --bootstrap-server=kafka:29092 --partitions=3 --replication-factor=1 &&
        kafka-topics --create --topic=order-events --if-not-exists --bootstrap-server=kafka:29092 --partitions=5 --replication-factor=1 &&
        kafka-topics --create --topic=notifications --if-not-exists --bootstrap-server=kafka:29092 --partitions=2 --replication-factor=1 &&
        echo 'Topics created successfully!' &&
        kafka-topics --list --bootstrap-server=kafka:29092"

  # Optional: Kafka UI for easy management
  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: kafka-ui
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: local
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:29092
      KAFKA_CLUSTERS_0_ZOOKEEPER: zookeeper:2181