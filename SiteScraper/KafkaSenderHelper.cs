using System.Text.Json;
using Confluent.Kafka;

namespace SiteScraper;

public class KafkaSenderHelper
{
    private readonly IConfiguration _configuration;
    //private string scrapingDataTopic;
    private readonly IProducer<Null, string>? _producer;

    public KafkaSenderHelper(IConfiguration configuration)
    {
        _configuration = configuration;
        _producer = CreateProducer();
        //scrapingDataTopic = _configuration["Kafka:ProducerTopic"] ?? "scraping-data";

    }
    private IProducer<Null, string> CreateProducer()
    {
        return new ProducerBuilder<Null, string>(CreateKafkaProducerConfig()).Build();
    }

    private ProducerConfig CreateKafkaProducerConfig()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ??
                               "localhost:30092",
            CompressionType = CompressionType.Gzip,
            MessageMaxBytes = 52428800,
        };
        return producerConfig;
    }

    public async Task CreateDeadLetterMsg(CancellationToken stoppingToken, ConsumeResult<Null, string> result, string errorMsg)
    {
        var deadLetter = new
        {
            OriginalMessage = result.Message.Value,
            ServiceName = "SiteScraper",
            Error = errorMsg,
            Timestamp = DateTime.UtcNow
        };
        var deadLetterTopic = _configuration["Kafka:DeadLetterTopic"] ?? "dead-letter-topic";
        await WriteMessageToKafka(stoppingToken, deadLetter, deadLetterTopic);
    }

    public async Task WriteMessageToKafka(CancellationToken stoppingToken, object msg, string topic)
    {
        var dataJson = JsonSerializer.Serialize(msg);
        var message = new Message<Null, string> { Value = dataJson };
        //var messageSize = System.Text.Encoding.UTF8.GetByteCount(scrapingDataJson);
        //_logger.LogWarning("Kafka message size: {Size} bytes", messageSize);
        await _producer.ProduceAsync(topic, message, stoppingToken);
    }

}