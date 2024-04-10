using System;
using System.Threading;

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

        return GeneratePartitionKey(utcEventTime, partitionKeyRounding);
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
        return GenerateRowKey(utcEventTime);
    }



    /// <summary>
    /// Generates the PartitionKey based on the logEvent timestamp
    /// </summary>
    /// <param name="utcEventTime">The UTC event time.</param>
    /// <param name="roundSpan">The round span.</param>
    /// <returns>
    /// The Generated PartitionKey
    /// </returns>
    /// <remarks>
    /// The partition key based on the Timestamp rounded to the nearest 5 min
    /// </remarks>
    public static string GeneratePartitionKey(DateTime utcEventTime, TimeSpan? roundSpan = null)
    {
        var span = roundSpan ?? TimeSpan.FromMinutes(5);
        var roundedEvent = utcEventTime.Round(span);

        // create a 19 character String for reverse chronological ordering.
        return $"{DateTime.MaxValue.Ticks - roundedEvent.Ticks:D19}";
    }

    /// <summary>
    /// Generates the RowKey using the timestamp
    /// </summary>
    /// <param name="utcEventTime">The UTC event time.</param>
    /// <returns>
    /// The generated RowKey
    /// </returns>
    public static string GenerateRowKey(DateTime utcEventTime)
    {
        // create a reverse chronological ordering date, newest logs sorted first
        var timestamp = utcEventTime.ToReverseChronological();

        // use Ulid for speed and efficiency
        return Ulid.NewUlid(timestamp).ToString();
    }
}
