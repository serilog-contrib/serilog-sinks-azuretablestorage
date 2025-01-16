using System;
using System.Collections.Generic;

using Serilog.Sinks.AzureTableStorage.Extensions;

using Xunit;
using Xunit.Abstractions;

namespace Serilog.Sinks.AzureTableStorage.Tests;

public class DefaultKeyGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public DefaultKeyGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }


    [Fact]
    public void GenerateRowKeyDateTimeOffsetNow()
    {
        var dateTime = new DateTimeOffset(2024, 4, 1, 23, 0, 0, TimeSpan.FromHours(-5));

        var rowKey = DefaultKeyGenerator.GenerateRowKey(dateTime);
        Assert.NotNull(rowKey);

        var parsed = Ulid.TryParse(rowKey, out var ulid);
        Assert.True(parsed);
        Assert.NotEqual(default, ulid);

        var reversed = dateTime.ToUniversalTime().ToReverseChronological();
        var ulidDate = ulid.Time;

        Assert.Equal(reversed.Year, ulidDate.Year);
        Assert.Equal(reversed.Month, ulidDate.Month);
        Assert.Equal(reversed.Day, ulidDate.Day);
        Assert.Equal(reversed.Hour, ulidDate.Hour);
        Assert.Equal(reversed.Minute, ulidDate.Minute);
    }


    [Fact]
    public void GeneratePartitionKeyDateTimeOffsetNow()
    {
        var dateTime = new DateTimeOffset(2024, 4, 1, 23, 0, 0, TimeSpan.FromHours(-5));

        var partitionKey = DefaultKeyGenerator.GeneratePartitionKey(dateTime);
        Assert.NotNull(partitionKey);
        Assert.Equal("2516902703999999999", partitionKey);
    }

    [Fact]
    public void GeneratePartitionKeyDateTimeNow()
    {
        var dateTime = new DateTimeOffset(2024, 4, 1, 23, 0, 0, TimeSpan.FromHours(-5));
        var eventTime = dateTime.UtcDateTime;

        var partitionKey = DefaultKeyGenerator.GeneratePartitionKey(eventTime);
        Assert.NotNull(partitionKey);
        Assert.Equal("2516902703999999999", partitionKey);
    }

    [Theory]
    [MemberData(nameof(GetDateRounding))]
    public void GeneratePartitionKeyDateTimeNowRound(DateTimeOffset dateTime, string expected)
    {
        var partitionKey = DefaultKeyGenerator.GeneratePartitionKey(dateTime);
        Assert.NotNull(partitionKey);
        Assert.Equal(expected, partitionKey);
    }

    public static IEnumerable<object[]> GetDateRounding()
    {
        yield return new object[]
        {
            new DateTimeOffset(2024, 4, 1, 23, 1, 0, TimeSpan.FromHours(-5)),
            "2516902703999999999"
        };
        yield return new object[]
        {
            new DateTimeOffset(2024, 4, 1, 23, 2, 55, TimeSpan.FromHours(-5)),
            "2516902700999999999"
        };
        yield return new object[]
        {
            new DateTimeOffset(2024, 4, 1, 23, 3, 5, TimeSpan.FromHours(-5)),
            "2516902700999999999"
        };
        yield return new object[]
        {
            new DateTimeOffset(2024, 4, 1, 23, 4, 11, TimeSpan.FromHours(-5)),
            "2516902700999999999"
        };
        yield return new object[]
        {
            new DateTimeOffset(2024, 4, 1, 23, 4, 43, TimeSpan.FromHours(-5)),
            "2516902700999999999"
        };
    }

    [Fact]
    public void GeneratePartitionKeyQueryDateOnly()
    {
        var date = new DateOnly(2024, 4, 1);

        var partitionKeyQuery = DefaultKeyGenerator.GeneratePartitionKeyQuery(date, TimeSpan.FromHours(-5));
        Assert.NotNull(partitionKeyQuery);
        Assert.Equal("(PartitionKey ge '2516902667999999999') and (PartitionKey lt '2516903531999999999')", partitionKeyQuery);
    }

    [Fact]
    public void GeneratePartitionKeyQueryDateTime()
    {
        var startDate = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.FromHours(-5));
        var startTime = startDate.UtcDateTime;

        var endDate = new DateTimeOffset(2024, 4, 2, 0, 0, 0, TimeSpan.FromHours(-5));
        var endTime = endDate.UtcDateTime;

        var partitionKeyQuery = DefaultKeyGenerator.GeneratePartitionKeyQuery(startTime, endTime);
        Assert.NotNull(partitionKeyQuery);
        Assert.Equal("(PartitionKey ge '2516902667999999999') and (PartitionKey lt '2516903531999999999')", partitionKeyQuery);
    }

    [Fact]
    public void GeneratePartitionKeyQueryDateTimeOffset()
    {
        var startTime = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.FromHours(-5));
        var endTime = new DateTimeOffset(2024, 4, 2, 0, 0, 0, TimeSpan.FromHours(-5));

        var partitionKeyQuery = DefaultKeyGenerator.GeneratePartitionKeyQuery(startTime, endTime);
        Assert.NotNull(partitionKeyQuery);
        Assert.Equal("(PartitionKey ge '2516902667999999999') and (PartitionKey lt '2516903531999999999')", partitionKeyQuery);
    }

}
