using System;

namespace Serilog.Sinks.AzureTableStorage.Extensions;

#if NET6_0_OR_GREATER
/// <summary>
/// Extension methods for <see cref="DateOnly"/>
/// </summary>
public static class DateOnlyExtensions
{
    /// <summary>
    /// Converts the <see cref="DateOnly"/> to a <see cref="DateTimeOffset"/> in the specified timezone.
    /// </summary>
    /// <param name="dateOnly">The date to convert.</param>
    /// <param name="zone">The time zone the date is in.</param>
    /// <returns>The converted <see cref="DateTimeOffset"/></returns>
    public static DateTimeOffset ToDateTimeOffset(this DateOnly dateOnly, TimeZoneInfo zone = null)
    {
        zone ??= TimeZoneInfo.Local;

        var dateTime = dateOnly.ToDateTime(TimeOnly.MinValue);
        var offset = zone.GetUtcOffset(dateTime);

        return new DateTimeOffset(dateTime, offset);
    }

    /// <summary>
    /// Converts the <see cref="DateTimeOffset"/> to a <see cref="DateOnly"/> in the specified timezone.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTimeOffset"/> to convert.</param>
    /// <param name="zone">The time zone the date is in.</param>
    /// <returns>The converted <see cref="DateOnly"/></returns>
    public static DateOnly ToDateOnly(this DateTimeOffset dateTime, TimeZoneInfo zone = null)
    {
        zone ??= TimeZoneInfo.Local;

        var targetZone = TimeZoneInfo.ConvertTime(dateTime, zone);
        return DateOnly.FromDateTime(targetZone.Date);
    }
}
#endif
