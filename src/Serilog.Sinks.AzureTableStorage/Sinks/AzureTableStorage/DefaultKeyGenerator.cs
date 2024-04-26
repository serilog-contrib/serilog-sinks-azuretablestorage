using System;
using System.Dynamic;
using System.Threading;

using Azure.Data.Tables;

using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.Extensions;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Default document key generator
/// </summary>
/// <seealso cref="Serilog.Sinks.AzureTableStorage.IKeyGenerator" />
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

        var utcEventTime = logEvent.Timestamp;
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

        var utcEventTime = logEvent.Timestamp;
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
        var span = roundSpan ?? TimeSpan.FromMinutes(5);
        var dateTime = eventTime.ToUniversalTime();
        var roundedEvent = dateTime.Round(span);

        // create a 19 character String for reverse chronological ordering.
        return $"{DateTimeOffset.MaxValue.Ticks - roundedEvent.Ticks:D19}";
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
        var dateTime = eventTime.ToUniversalTime();
        var dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);

        return GeneratePartitionKey(dateTimeOffset, roundSpan);
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
        var dateTime = eventTime.ToUniversalTime();

        // create a reverse chronological ordering date, newest logs sorted first
        var timestamp = dateTime.ToReverseChronological();

        // use Ulid for speed and efficiency
        return Ulid.NewUlid(timestamp).ToString();
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
        var dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);

        return GenerateRowKey(dateTimeOffset);
    }


#if NET6_0_OR_GREATER
    /// <summary>
    /// Generates the partition key query using the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date to use for query.</param>
    /// <param name="offset">The date's offset from Coordinated Universal Time (UTC).</param>
    /// <returns>An Azure Table partiion key query.</returns>
    public static string GeneratePartitionKeyQuery(DateOnly date, TimeSpan offset)
    {
        // date is assumed to be in local time, will be converted to UTC
        var startTime = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, offset);
        var endTime = startTime.AddDays(1);

        return GeneratePartitionKeyQuery(startTime, endTime);
    }

    /// <summary>
    /// Generates the partition key query using the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date to use for query.</param>
    /// <param name="zone">The time zone the date is in.</param>
    /// <returns>An Azure Table partiion key query.</returns>
    public static string GeneratePartitionKeyQuery(DateOnly date, TimeZoneInfo zone = null)
    {
        // date is assumed to be in local time, will be converted to UTC
        var startTime = date.ToDateTimeOffset(zone);
        var endTime = date.AddDays(1).ToDateTimeOffset(zone);

        return GeneratePartitionKeyQuery(startTime, endTime);
    }
#endif

    /// <summary>
    /// Generates the partition key query using the specified <paramref name="startDate"/> and <paramref name="endDate"/>.
    /// </summary>
    /// <param name="startDate">The start date to use for query.</param>
    /// <param name="endDate">The end date to use for query.</param>
    /// <returns>An Azure Table partiion key query.</returns>
    public static string GeneratePartitionKeyQuery(DateTime startDate, DateTime endDate)
    {
        var startTime = startDate.ToUniversalTime();
        var startTimeOffset = new DateTimeOffset(startTime, TimeSpan.Zero);

        var endTime = endDate.ToUniversalTime();
        var endTimeOffset = new DateTimeOffset(endTime, TimeSpan.Zero);

        return GeneratePartitionKeyQuery(startTimeOffset, endTimeOffset);
    }

    /// <summary>
    /// Generates the partition key query using the specified <paramref name="startDate"/> and <paramref name="endDate"/>.
    /// </summary>
    /// <param name="startDate">The start date to use for query.</param>
    /// <param name="endDate">The end date to use for query.</param>
    /// <returns>An Azure Table partiion key query.</returns>
    public static string GeneratePartitionKeyQuery(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var startTime = startDate.ToUniversalTime();
        var endTime = endDate.ToUniversalTime();

        var upper = startTime.ToReverseChronological().Ticks.ToString("D19");
        var lower = endTime.ToReverseChronological().Ticks.ToString("D19");

        return $"({PartitionKeyName} ge '{lower}') and ({PartitionKeyName} lt '{upper}')";
    }
}
