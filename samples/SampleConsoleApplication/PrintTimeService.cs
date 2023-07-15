using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleConsoleApplication;

public class PrintTimeService : BackgroundService
{
    private readonly ILogger<PrintTimeService> _logger;

    public PrintTimeService(ILogger<PrintTimeService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("The current time is: {CurrentTime}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
