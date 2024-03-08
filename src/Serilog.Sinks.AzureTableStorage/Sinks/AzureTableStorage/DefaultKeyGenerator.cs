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
        var roundedEvent = Round(utcEventTime, span);

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
        // create a reverse chronological ordering date
        var targetTicks = DateTime.MaxValue.Ticks - utcEventTime.Ticks;

        // add incrementing value to ensure unique
        int padding = Next();

        return $"{targetTicks:D19}{padding:D4}";
    }

    /// <summary>
    /// Rounds the specified date.
    /// </summary>
    /// <param name="date">The date to round.</param>
    /// <param name="span">The span.</param>
    /// <returns>The rounded date</returns>
    public static DateTime Round(DateTime date, TimeSpan span)
    {
        long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
        return new DateTime(ticks * span.Ticks);
    }


    private static int _counter = new Random().Next(_minCounter, _maxCounter);

    private const int _minCounter = 1;
    private const int _maxCounter = 9999;

    private static int Next()
    {
        Interlocked.Increment(ref _counter);
        return Interlocked.CompareExchange(ref _counter, _minCounter, _maxCounter);
    }

}
