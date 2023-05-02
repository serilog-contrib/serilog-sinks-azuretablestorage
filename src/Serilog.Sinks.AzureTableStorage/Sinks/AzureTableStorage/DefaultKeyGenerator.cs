using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.Extensions;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Default document key generator
/// </summary>
public class DefaultKeyGenerator : IKeyGenerator
{
    private readonly AzureTableStorageSinkOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultKeyGenerator"/> class.
    /// </summary>
    /// <param name="options">The table storage options.</param>
    public DefaultKeyGenerator(AzureTableStorageSinkOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Automatically generates the PartitionKey based on the logEvent timestamp
    /// </summary>
    /// <param name="logEvent">the log event</param>
    /// <returns>The Generated PartitionKey</returns>
    /// <remarks>The partition key based on the Timestamp rounded to the nearest 5 min</remarks>
    public virtual string GeneratePartitionKey(LogEvent logEvent)
    {
        // log entries are partitioned in 5 minute blocks.
        // batch insert is used to get around time based partition key performance issues
        // values are created in reverse chronological order so newest are always first

        var utcEventTime = logEvent.Timestamp.UtcDateTime;
        return utcEventTime.GeneratePartitionKey(_options.PartitionKeyRounding);
    }

    /// <summary>
    /// Automatically generates the RowKey using the timestamp 
    /// </summary>
    /// <param name="logEvent">the log event</param>
    /// <returns>The generated RowKey</returns>
    public virtual string GenerateRowKey(LogEvent logEvent)
    {
        // row key created in reverse chronological order so newest are always first

        var utcEventTime = logEvent.Timestamp.UtcDateTime;
        return utcEventTime.GenerateRowKey();
    }
}

