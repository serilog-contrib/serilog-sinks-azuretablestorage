using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleStressTest;

public class StressTestService : IHostedLifecycleService
{
    private readonly ILogger<StressTestService> _logger;

    public StressTestService(ILogger<StressTestService> logger)
    {
        this._logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartAsync Called");
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartedAsync Called");

        // hammer the logger
        var result = Parallel.For(1, 10000, index =>
        {
            _logger.LogInformation("Logging Loop {index}", index);
            _logger.LogInformation("Another Entry {index}", index);
            _logger.LogInformation("Duplicate Entry {index}", index);
        });

        _logger.LogInformation("Stress test: {completed}", result.IsCompleted);

        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartingAsync Called");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync Called");
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StoppedAsync Called");
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StoppingAsync Called");
        return Task.CompletedTask;
    }

}
