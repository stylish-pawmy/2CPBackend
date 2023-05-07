namespace _2cpbackend.Services;

using Microsoft.EntityFrameworkCore;

using _2cpbackend.Models;
using _2cpbackend.Data;

public class EventArchiver : BackgroundService
{
    private readonly ILogger<BackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    public EventArchiver(ILogger<BackgroundService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = 1;
        var refreshRate = (int) TimeSpan.FromMinutes(minutes).TotalMilliseconds;

        while(!stoppingToken.IsCancellationRequested)
        {
            //Resolving scope problems by creating a scope only at treatement time
            using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var events = _context.Events.ToList();

                foreach(Event _event in events)
                {
                    if (_event.DateAndTime + _event.TimeSpan < DateTime.UtcNow)
                        _event.Status = EventStatus.Ended;
                    else if (_event.DateAndTime < DateTime.UtcNow)
                        _event.Status = EventStatus.Current;

                    _logger.LogInformation($"Archived event with Id {_event.Id}");
                }

                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Archive refreshed at: {DateTime.Now.ToString()} ");
            await Task.Delay(refreshRate, stoppingToken);
        }
    }
}