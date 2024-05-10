using System;
using System.Collections.Generic;

using FluentAssertions;

using Serilog.Sinks.AzureTableStorage.Extensions;

using Xunit;
using Xunit.Abstractions;

namespace Serilog.Sinks.AzureTableStorage.Tests;

public class DateTimeExtensionsTests
{
    private readonly ITestOutputHelper _output;

    public DateTimeExtensionsTests(ITestOutputHelper output)
    {
        _output = output;
    }


    [Theory]
    [MemberData(nameof(GetDateRounding))]
    public void GeneratePartitionKeyDateTimeNowRound(DateTime dateTime, DateTime expected, TimeSpan span)
    {
        var rounded = DateTimeExtensions.Round(dateTime, span);
        rounded.Should().Be(expected);
    }

    public static IEnumerable<object[]> GetDateRounding()
    {
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 1, 0, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 0, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 1, 59, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 0, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 2, 29, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 0, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 2, 31, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 5, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 2, 55, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 5, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 3, 5, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 5, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 4, 11, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 5, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
        yield return new object[]
        {
            new DateTime(2024, 4, 1, 23, 4, 43, DateTimeKind.Local),
            new DateTime(2024, 4, 1, 23, 5, 0, DateTimeKind.Local),
            TimeSpan.FromMinutes(5),
        };
    }

}
