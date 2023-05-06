namespace _2cpbackend.Services;

public class EventArchiver : BackgroundService
{
    private readonly ILogger<BackgroundService> _logger;
    public EventArchiver(ILogger<BackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation($"Archiver listening | {DateTime.Now.ToString()} ");
            await Task.Delay(1000, stoppingToken);
        }
    }
}