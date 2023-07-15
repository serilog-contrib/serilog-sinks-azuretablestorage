using System;

using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.Extensions;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Default document key generator
/// </summary>
public class DefaultKeyGenerator : IKeyGenerator
{
    /// <summary>
    /// Automatically generates the PartitionKey based on the logEvent timestamp
    /// </summary>
    /// <param name="logEvent">the log event</param>
    /// <param name="options">The table storage options.</param>
    /// <returns>The Generated PartitionKey</returns>
    /// <remarks>The partition key based on the Timestamp rounded to the nearest 5 min</remarks>
    public virtual string GeneratePartitionKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
    {
        if (logEvent is null)
            throw new ArgumentNullException(nameof(logEvent));

        // log entries are partitioned in 5 minute blocks.
        // batch insert is used to get around time based partition key performance issues
        // values are created in reverse chronological order so newest are always first

        var utcEventTime = logEvent.Timestamp.UtcDateTime;
        var partitionKeyRounding = options?.PartitionKeyRounding;

        return utcEventTime.GeneratePartitionKey(partitionKeyRounding);
    }

    /// <summary>
    /// Automatically generates the RowKey using the timestamp 
    /// </summary>
    /// <param name="logEvent">the log event</param>
    /// <param name="options">The table storage options.</param>
    /// <returns>The generated RowKey</returns>
    public virtual string GenerateRowKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
    {
        if (logEvent is null)
            throw new ArgumentNullException(nameof(logEvent));

        // row key created in reverse chronological order so newest are always first

        var utcEventTime = logEvent.Timestamp.UtcDateTime;
        return utcEventTime.GenerateRowKey();
    }
}
