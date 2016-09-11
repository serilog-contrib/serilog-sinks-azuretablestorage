﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Xunit;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Serilog.Sinks.AzureTableStorage.Tests
{
    public class AzureTableStorageWithPropertiesSinkTests : IDisposable
    {
        private readonly bool _wasStorageEmulatorUp;

        public AzureTableStorageWithPropertiesSinkTests()
        {
            if (!AzureStorageEmulatorManager.IsProcessStarted())
            {
                AzureStorageEmulatorManager.StartStorageEmulator();
                _wasStorageEmulatorUp = false;
            }
            else
            {
                _wasStorageEmulatorUp = true;
            }

        }

        public void Dispose()
        {
            if (!_wasStorageEmulatorUp)
            {
                AzureStorageEmulatorManager.StopStorageEmulator();
            }
            else
            {
                // Leave as it was before testing...
            }
        }

        static async Task<IList<DynamicTableEntity>> TableQueryTakeDynamicAsync(CloudTable table, int takeCount)
        {
            var queryToken = new TableContinuationToken();
            var result = await table.ExecuteQuerySegmentedAsync(new TableQuery().Take(takeCount), queryToken);
            return result.Results;
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItIsRetrievableFromTheTableWithProperties()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var exception = new ArgumentException("Some exception");

            const string messageTemplate = "{Properties} should go in their {Numbered} {Space}";

            logger.Information(exception, messageTemplate, "Properties", 1234, ' ');

            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            // Check the presence of same properties as in previous version
            Assert.Equal(messageTemplate, result.Properties["MessageTemplate"].StringValue);
            Assert.Equal("Information", result.Properties["Level"].StringValue);
            Assert.Equal("System.ArgumentException: Some exception", result.Properties["Exception"].StringValue);
            Assert.Equal("\"Properties\" should go in their 1234  ", result.Properties["RenderedMessage"].StringValue);

            // Check the presence of the new properties.
            Assert.Equal("Properties", result.Properties["Properties"].PropertyAsObject);
            Assert.Equal(1234, result.Properties["Numbered"].PropertyAsObject);
            Assert.Equal(" ", result.Properties["Space"].PropertyAsObject);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItStoresTheCorrectTypesForScalar()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var bytearrayValue = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 250, 251, 252, 253, 254, 255 };
            var booleanValue = true;
            var datetimeValue = DateTime.UtcNow;
            var datetimeoffsetValue = new DateTimeOffset(datetimeValue, TimeSpan.FromHours(0));
            var doubleValue = Math.PI;
            var guidValue = Guid.NewGuid();
            var intValue = int.MaxValue;
            var longValue = long.MaxValue;
            var stringValue = "Some string value";

            logger.Information("{ByteArray} {Boolean} {DateTime} {DateTimeOffset} {Double} {Guid} {Int} {Long} {String}",
                bytearrayValue,
                booleanValue,
                datetimeValue,
                datetimeoffsetValue,
                doubleValue,
                guidValue,
                intValue,
                longValue,
                stringValue);

            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal(bytearrayValue, result.Properties["ByteArray"].BinaryValue);
            Assert.Equal(booleanValue, result.Properties["Boolean"].BooleanValue);
            Assert.Equal(datetimeValue, result.Properties["DateTime"].DateTime);
            Assert.Equal(datetimeoffsetValue, result.Properties["DateTimeOffset"].DateTimeOffsetValue);
            Assert.Equal(doubleValue, result.Properties["Double"].DoubleValue);
            Assert.Equal(guidValue, result.Properties["Guid"].GuidValue);
            Assert.Equal(intValue, result.Properties["Int"].Int32Value);
            Assert.Equal(longValue, result.Properties["Long"].Int64Value);
            Assert.Equal(stringValue, result.Properties["String"].StringValue);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItStoresTheCorrectTypesForDictionary()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var dict1 = new Dictionary<string, string>{
                {"d1k1", "d1k1v1"},
                {"d1k2", "d1k2v2"},
                {"d1k3", "d1k3v3"}
            };

            var dict2 = new Dictionary<string, string>{
                {"d2k1", "d2k1v1"},
                {"d2k2", "d2k2v2"},
                {"d2k3", "d2k3v3"}
            };

            var dict0 = new Dictionary<string, Dictionary<string, string>>{
                 {"d1", dict1},
                 {"d2", dict2}
            };

            logger.Information("{Dictionary}", dict0);
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal("[(\"d1\": [(\"d1k1\": \"d1k1v1\"), (\"d1k2\": \"d1k2v2\"), (\"d1k3\": \"d1k3v3\")]), (\"d2\": [(\"d2k1\": \"d2k1v1\"), (\"d2k2\": \"d2k2v2\"), (\"d2k3\": \"d2k3v3\")])]", result.Properties["Dictionary"].StringValue);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItStoresTheCorrectTypesForSequence()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var seq1 = new int[] { 1, 2, 3, 4, 5 };
            var seq2 = new string[] { "a", "b", "c", "d", "e" };

            logger.Information("{Seq1} {Seq2}", seq1, seq2);
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal("[1, 2, 3, 4, 5]", result.Properties["Seq1"].StringValue);
            Assert.Equal("[\"a\", \"b\", \"c\", \"d\", \"e\"]", result.Properties["Seq2"].StringValue);
        }

        private class Struct1
        {
            public int IntVal { get; set; }
            public string StringVal { get; set; }
        }

        private class Struct2
        {
            public DateTime DateTimeVal { get; set; }
            public double DoubleVal { get; set; }
        }

        private class Struct0
        {
            public Struct1 Struct1Val { get; set; }
            public Struct2 Struct2Val { get; set; }
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItStoresTheCorrectTypesForStructure()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var struct1 = new Struct1
            {
                IntVal = 10,
                StringVal = "ABCDE"
            };

            var struct2 = new Struct2
            {
                DateTimeVal = new DateTime(2014, 12, 3, 17, 37, 12),
                DoubleVal = Math.PI
            };

            var struct0 = new Struct0
            {
                Struct1Val = struct1,
                Struct2Val = struct2
            };

            logger.Information("{@Struct0}", struct0);
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal("Struct0 { Struct1Val: Struct1 { IntVal: 10, StringVal: \"ABCDE\" }, Struct2Val: Struct2 { DateTimeVal: 12/03/2014 17:37:12, DoubleVal: 3.14159265358979 } }", result.Properties["Struct0"].StringValue);
        }

        [Fact]
        public async Task WhenABatchLoggerWritesToTheSinkItStoresAllTheEntries()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            using(var sink = new AzureBatchingTableStorageWithPropertiesSink(storageAccount, null, 1000, TimeSpan.FromMinutes(1)))
            {
                var timestamp = new DateTimeOffset(2014, 12, 01, 18, 42, 20, 666, TimeSpan.FromHours(2));
                var messageTemplate = "Some text";
                var template = new MessageTemplateParser().Parse(messageTemplate);
                var properties = new List<LogEventProperty>();
                for (int i = 0; i < 10; ++i)
                {
                    sink.Emit(new Events.LogEvent(timestamp, LogEventLevel.Information, null, template, properties));
                }
            }

            var result = await TableQueryTakeDynamicAsync(table, takeCount: 11);
            Assert.Equal(10, result.Count);
        }

        [Fact]
        public async Task WhenABatchLoggerWritesToTheSinkItStoresAllTheEntriesInDifferentPartitions()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            using (var sink = new AzureBatchingTableStorageWithPropertiesSink(storageAccount, null, 1000, TimeSpan.FromMinutes(1)))
            {
                var messageTemplate = "Some text";
                var template = new MessageTemplateParser().Parse(messageTemplate);
                var properties = new List<LogEventProperty>();

                for(int k = 0; k < 4; ++k)
                {
                    var timestamp = new DateTimeOffset(2014, 12, 01, 1+k, 42, 20, 666, TimeSpan.FromHours(2));
                    for (int i = 0; i < 2; ++i)
                    {
                        sink.Emit(new Events.LogEvent(timestamp, LogEventLevel.Information, null, template, properties));
                    }
                }
            }

            var result = await TableQueryTakeDynamicAsync(table, takeCount: 9);
            Assert.Equal(8, result.Count);
        }

        [Fact]
        public async Task WhenABatchLoggerWritesToTheSinkItStoresAllTheEntriesInLargeNumber()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            using (var sink = new AzureBatchingTableStorageWithPropertiesSink(storageAccount, null, 1000, TimeSpan.FromMinutes(1)))
            {
                var timestamp = new DateTimeOffset(2014, 12, 01, 18, 42, 20, 666, TimeSpan.FromHours(2));
                var messageTemplate = "Some text";
                var template = new MessageTemplateParser().Parse(messageTemplate);
                var properties = new List<LogEventProperty>();
                for (int i = 0; i < 300; ++i)
                {
                    sink.Emit(new Events.LogEvent(timestamp, LogEventLevel.Information, null, template, properties));
                }
            }

            var result = await TableQueryTakeDynamicAsync(table, takeCount: 301);
            Assert.Equal(300, result.Count);
        }

    }
}
