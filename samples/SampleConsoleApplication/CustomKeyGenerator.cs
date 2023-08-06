using Serilog.Events;
using Serilog.Sinks.AzureTableStorage;
using Serilog.Sinks.AzureTableStorage.Extensions;

namespace SampleConsoleApplication;

public class CustomKeyGenerator : IKeyGenerator
{
    public virtual string GeneratePartitionKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
    {
        var utcEventTime = logEvent.Timestamp.UtcDateTime;
        return utcEventTime.GeneratePartitionKey();
    }

    public virtual string GenerateRowKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
    {
        var utcEventTime = logEvent.Timestamp.UtcDateTime;
        return utcEventTime.GenerateRowKey();
    }
}
