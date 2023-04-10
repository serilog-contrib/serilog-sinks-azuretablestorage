using System;

using Serilog.Events;

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
    /// <returns>The Generated PartitionKey</returns>
    /// <remarks>The partition key based on the Timestamp rounded to the nearest 5 min</remarks>
    public virtual string GeneratePartitionKey(LogEvent logEvent)
    {
        // log entries are partitioned in 5 minute blocks.
        // batch insert is used to get around time based partition key performance issues
        // values are created in reverse chronological order so newest are always first

        var utcEventTime = logEvent.Timestamp.UtcDateTime;
        var roundedEvent = Round(utcEventTime, TimeSpan.FromMinutes(5));

        // create a 19 character String for reverse chronological ordering.
        return $"{DateTime.MaxValue.Ticks - roundedEvent.Ticks:D19}";
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

        // create a 19 character String for reverse chronological ordering.
        return $"{DateTime.MaxValue.Ticks - utcEventTime.Ticks:D19}";
    }


    private static DateTime Round(DateTime date, TimeSpan span)
    {
        long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
        return new DateTime(ticks * span.Ticks);
    }
}

