using System;
using System.Threading;

using Azure.Data.Tables;

using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.Extensions;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Default document key generator
/// </summary>
public class DefaultKeyGenerator : IKeyGenerator
{
    private const string PartitionKeyName = nameof(ITableEntity.PartitionKey);

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
    /// Generates the PartitionKey based on the specified <paramref name="eventTime"/> timestamp
    /// </summary>
    /// <param name="eventTime">The event time.</param>
    /// <param name="roundSpan">The round span.</param>
    /// <returns>
    /// The Generated PartitionKey
    /// </returns>
    /// <remarks>
    /// The partition key based on the Timestamp rounded to the nearest 5 min
    /// </remarks>
    public static string GeneratePartitionKey(DateTimeOffset eventTime, TimeSpan? roundSpan = null)
    {
        return GeneratePartitionKey(eventTime.UtcDateTime, roundSpan);
    }

    /// <summary>
    /// Generates the PartitionKey based on the specified <paramref name="eventTime"/> timestamp
    /// </summary>
    /// <param name="eventTime">The event time.</param>
    /// <param name="roundSpan">The round span.</param>
    /// <returns>
    /// The Generated PartitionKey
    /// </returns>
    /// <remarks>
    /// The partition key based on the Timestamp rounded to the nearest 5 min
    /// </remarks>
    public static string GeneratePartitionKey(DateTime eventTime, TimeSpan? roundSpan = null)
    {
        var span = roundSpan ?? TimeSpan.FromMinutes(5);
        var dateTime = eventTime.ToUniversalTime();
        var roundedEvent = dateTime.Round(span);

        // create a 19 character String for reverse chronological ordering.
        return $"{DateTime.MaxValue.Ticks - roundedEvent.Ticks:D19}";
    }


    /// <summary>
    /// Generates the RowKey using a reverse chronological ordering date, newest logs sorted first
    /// </summary>
    /// <param name="eventTime">The event time.</param>
    /// <returns>
    /// The generated RowKey
    /// </returns>
    public static string GenerateRowKey(DateTimeOffset eventTime)
    {
        return GenerateRowKey(eventTime.UtcDateTime);
    }

    /// <summary>
    /// Generates the RowKey using a reverse chronological ordering date, newest logs sorted first
    /// </summary>
    /// <param name="eventTime">The event time.</param>
    /// <returns>
    /// The generated RowKey
    /// </returns>
    public static string GenerateRowKey(DateTime eventTime)
    {
        var dateTime = eventTime.ToUniversalTime();

        // create a reverse chronological ordering date, newest logs sorted first
        var timestamp = dateTime.ToReverseChronological();

        // use Ulid for speed and efficiency
        return Ulid.NewUlid(timestamp).ToString();
    }


#if NET6_0_OR_GREATER
    /// <summary>
    /// Generates the partition key query using the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date to use for query.</param>
    /// <returns>An Azure Table partiion key query.</returns>
    public static string GeneratePartitionKeyQuery(DateOnly date)
    {
        // date is assumed to be in local time, will be converted to UTC
        var eventTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);
        return GeneratePartitionKeyQuery(eventTime);
    }
#endif

    /// <summary>
    /// Generates the partition key query using the specified <paramref name="eventTime"/>.
    /// </summary>
    /// <param name="eventTime">The date to use for query.</param>
    /// <returns>An Azure Table partiion key query.</returns>
    public static string GeneratePartitionKeyQuery(DateTime eventTime)
    {
        var dateTime = eventTime.ToUniversalTime();

        var upper = dateTime.ToReverseChronological().Ticks.ToString("D19");
        var lower = dateTime.AddDays(1).ToReverseChronological().Ticks.ToString("D19");

        return $"({PartitionKeyName} ge '{lower}') and ({PartitionKeyName} lt '{upper}')";
    }

    /// <summary>
    /// Generates the partition key query using the specified <paramref name="eventTime"/>.
    /// </summary>
    /// <param name="eventTime">The date to use for query.</param>
    /// <returns>An Azure Table partiion key query.</returns>
    public static string GeneratePartitionKeyQuery(DateTimeOffset eventTime)
    {
        return GeneratePartitionKeyQuery(eventTime.UtcDateTime);
    }

}
