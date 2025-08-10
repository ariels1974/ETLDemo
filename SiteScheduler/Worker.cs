using System.Text.Json;

namespace SiteScheduler;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private List<SiteScheduleConfig> _configs = new();
    private KafkaProducerService? _kafkaProducer;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var configPath = _configuration["SchedulerConfigPath"] ?? "schedulerConfig.json";
        if (File.Exists(configPath))
        {
            _configs = SchedulerConfigLoader.Load(configPath);
            _logger.LogInformation("Loaded {Count} site schedules from {Path}", _configs.Count, configPath);
        }
        else
        {
            _logger.LogWarning("Scheduler config file not found: {Path}", configPath);
        }

        var kafkaBootstrap = _configuration["Kafka:BootstrapServers"] ?? "localhost:30092";
        var kafkaTopic = _configuration["Kafka:Topic"] ?? "scraping-requests";
        _kafkaProducer = new KafkaProducerService(kafkaBootstrap, kafkaTopic);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timers = new List<Timer>();
        foreach (var config in _configs)
        {
            var timer = new Timer(async _ =>
            {
                _logger.LogInformation("Scheduled task for {SiteName} at {SiteAddress}", config.SiteName, config.SiteAddress);
                if (_kafkaProducer != null)
                {
                    var json = JsonSerializer.Serialize(config);
                    await _kafkaProducer.ProduceAsync(json);
                    _logger.LogInformation("Sent SiteScheduleConfig to Kafka topic scraping-requests: {Json}", json);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(config.Period));
            timers.Add(timer);
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        finally
        {
            foreach (var timer in timers)
                await timer.DisposeAsync();
        }
    }
}
