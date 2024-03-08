using System;

namespace Serilog.Sinks.AzureTableStorage.Extensions;

/// <summary>
/// Extension methods for <see cref="DateTime"/>
/// </summary>
public static class DateTimeExtensions
{

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
    public static string GeneratePartitionKey(this DateTime utcEventTime, TimeSpan? roundSpan = null)
    {
        return DefaultKeyGenerator.GeneratePartitionKey(utcEventTime, roundSpan);
    }

    /// <summary>
    /// Generates the RowKey using the timestamp
    /// </summary>
    /// <param name="utcEventTime">The UTC event time.</param>
    /// <returns>
    /// The generated RowKey
    /// </returns>
    public static string GenerateRowKey(this DateTime utcEventTime)
    {
        return DefaultKeyGenerator.GenerateRowKey(utcEventTime);
    }

    /// <summary>
    /// Rounds the specified date.
    /// </summary>
    /// <param name="date">The date to round.</param>
    /// <param name="span">The span.</param>
    /// <returns>The rounded date</returns>
    public static DateTime Round(this DateTime date, TimeSpan span)
    {
        return DefaultKeyGenerator.Round(date, span);
    }
}
