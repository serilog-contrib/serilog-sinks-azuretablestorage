using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Data.Tables.Sas;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Sinks.AzureTableStorage.Tests
{
    [Collection("AzureStorageIntegrationTests")]
    public class AzureTableStorageWithPropertiesSinkTests
    {
        private const string DevelopmentStorageAccountConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";
        private const string DevelopmentStorageAccountTableEndpoint = "http://127.0.0.1:10002/devstoreaccount1/LogEventEntity";

        static async Task<IList<TableEntity>> TableQueryTakeDynamicAsync(TableClient table, int takeCount)
        {
            List<TableEntity> results = new List<TableEntity>();
            await foreach(var page in table.QueryAsync<TableEntity>( _=> true, takeCount).AsPages())
            {
                results.AddRange(page.Values);
            }
            return results;
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItIsRetrievableFromTheTableWithProperties()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var exception = new ArgumentException("Some exception");

            const string messageTemplate = "{Properties} should go in their {Numbered} {Space}";

            logger.Information(exception, messageTemplate, "Properties", 1234, ' ');

            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            // Check the presence of same properties as in previous version
            Assert.Equal(messageTemplate, result["MessageTemplate"]);
            Assert.Equal("Information", result["Level"]);
            Assert.Equal("System.ArgumentException: Some exception", result["Exception"]);
            Assert.Equal("\"Properties\" should go in their 1234  ", result["RenderedMessage"]);

            // Check the presence of the new properties.
            Assert.Equal("Properties", result["Properties"]);
            Assert.Equal(1234, result["Numbered"]);
            Assert.Equal(" ", result["Space"]);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkWithAWindowsNewlineInTheTemplateItIsRetrievable()
        {
            // Prompted from https://github.com/serilog/serilog-sinks-azuretablestorage/issues/10
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            const string messageTemplate = "Line 1\r\nLine2";

            logger.Information(messageTemplate);

            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkWithALineFeedInTheTemplateItIsRetrievable()
        {
            // Prompted from https://github.com/serilog/serilog-sinks-azuretablestorage/issues/10
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            const string messageTemplate = "Line 1\nLine2";

            logger.Information(messageTemplate);

            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkWithACarriageReturnInTheTemplateItIsRetrievable()
        {
            // Prompted from https://github.com/serilog/serilog-sinks-azuretablestorage/issues/10
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            const string messageTemplate = "Line 1\rLine2";

            logger.Information(messageTemplate);

            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItStoresTheCorrectTypesForScalar()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

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

            //Assert.Equal(bytearrayValue, result.Properties["ByteArray"].BinaryValue);
            Assert.Equal(booleanValue, result["Boolean"]);
            Assert.Equal(datetimeValue, ((DateTimeOffset)result["DateTime"]).DateTime);
            Assert.Equal(datetimeoffsetValue, result["DateTimeOffset"]);
            Assert.Equal(doubleValue, result["Double"]);
            Assert.Equal(guidValue, result["Guid"]);
            Assert.Equal(intValue, result["Int"]);
            Assert.Equal(longValue, result["Long"]);
            Assert.Equal(stringValue, result["String"]);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItStoresTheCorrectTypesForDictionary()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

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

            Assert.Equal("[(\"d1\": [(\"d1k1\": \"d1k1v1\"), (\"d1k2\": \"d1k2v2\"), (\"d1k3\": \"d1k3v3\")]), (\"d2\": [(\"d2k1\": \"d2k1v1\"), (\"d2k2\": \"d2k2v2\"), (\"d2k3\": \"d2k3v3\")])]", result["Dictionary"] as string);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItStoresTheCorrectTypesForSequence()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var seq1 = new int[] { 1, 2, 3, 4, 5 };
            var seq2 = new string[] { "a", "b", "c", "d", "e" };

            logger.Information("{Seq1} {Seq2}", seq1, seq2);
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal("[1, 2, 3, 4, 5]", result["Seq1"]);
            Assert.Equal("[\"a\", \"b\", \"c\", \"d\", \"e\"]", result["Seq2"]);
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
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

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

#if NET472
            Assert.Equal("Struct0 { Struct1Val: Struct1 { IntVal: 10, StringVal: \"ABCDE\" }, Struct2Val: Struct2 { DateTimeVal: 12/03/2014 17:37:12, DoubleVal: 3.14159265358979 } }", result["Struct0"]);
#else
            Assert.Equal("Struct0 { Struct1Val: Struct1 { IntVal: 10, StringVal: \"ABCDE\" }, Struct2Val: Struct2 { DateTimeVal: 12/03/2014 17:37:12, DoubleVal: 3.141592653589793 } }", result["Struct0"]);
#endif
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItAllowsStringFormatNumericPropertyNames()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var expectedResult = "Hello \"world\"";

            logger.Information("Hello {0}", "world");
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal(expectedResult, result["RenderedMessage"]);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItAllowsNamedAndNumericPropertyNames()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var name = "John Smith";
            var expectedResult = "Hello \"world\" this is \"John Smith\" 1234";

            logger.Information("Hello {0} this is {Name} {_1234}", "world", name, 1234);
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal(expectedResult, result["RenderedMessage"]);
            Assert.Equal(name, result["Name"]);
            Assert.Equal(1234, result["_1234"]);
        }

        [Fact]
        public async Task WhenABatchLoggerWritesToTheSinkItStoresAllTheEntries()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            using(var sink = new AzureBatchingTableStorageWithPropertiesSink(storageAccount, null, 1000, TimeSpan.FromMinutes(1)))
            {
                var timestamp = new DateTimeOffset(2014, 12, 01, 18, 42, 20, 666, TimeSpan.FromHours(2));
                var messageTemplate = "Some text";
                var template = new MessageTemplateParser().Parse(messageTemplate);
                var properties = new List<LogEventProperty>();
                for (int i = 0; i < 10; ++i)
                {
                    sink.Emit(new LogEvent(timestamp, LogEventLevel.Information, null, template, properties));
                }
            }

            var result = await TableQueryTakeDynamicAsync(table, takeCount: 11);
            Assert.Equal(10, result.Count);
        }

        [Fact]
        public async Task WhenABatchLoggerWritesToTheSinkItStoresAllTheEntriesInDifferentPartitions()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

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
                        sink.Emit(new LogEvent(timestamp, LogEventLevel.Information, null, template, properties));
                    }
                }
            }

            var result = await TableQueryTakeDynamicAsync(table, takeCount: 9);
            Assert.Equal(8, result.Count);
        }

        [Fact]
        public async Task WhenABatchLoggerWritesToTheSinkItStoresAllTheEntriesInLargeNumber()
        {
            var storageAccount = new TableServiceClient(DevelopmentStorageAccountConnectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();

            using (var sink = new AzureBatchingTableStorageWithPropertiesSink(storageAccount, null, 1000, TimeSpan.FromMinutes(1)))
            {
                var timestamp = new DateTimeOffset(2014, 12, 01, 18, 42, 20, 666, TimeSpan.FromHours(2));
                var messageTemplate = "Some text";
                var template = new MessageTemplateParser().Parse(messageTemplate);
                var properties = new List<LogEventProperty>();
                for (int i = 0; i < 300; ++i)
                {
                    sink.Emit(new LogEvent(timestamp, LogEventLevel.Information, null, template, properties));
                }
            }

            var result = await TableQueryTakeDynamicAsync(table, takeCount: 301);
            Assert.Equal(300, result.Count);
        }

        [Fact]
        public void WhenALoggerUsesAnUnreachableStorageServiceItDoesntFail()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorage("DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=/////////////////////////////////////////////////////////////////////////////////////w==;BlobEndpoint=http://127.0.0.1:16660/devstoreaccount1;TableEndpoint=http://127.0.0.1:16662/devstoreaccount1;QueueEndpoint=http://127.0.0.1:16661/devstoreaccount1;")
                .CreateLogger();

            Log.Information("This should silently work, even though the connection string points to invalid endpoints");

            Assert.True(true);
        }

        [Fact]
        public void WhenALoggerWithPropertiesUsesAnUnreachableStorageServiceItDoesntFail()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties("DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=/////////////////////////////////////////////////////////////////////////////////////w==;BlobEndpoint=http://127.0.0.1:16660/devstoreaccount1;TableEndpoint=http://127.0.0.1:16662/devstoreaccount1;QueueEndpoint=http://127.0.0.1:16661/devstoreaccount1;")
                .CreateLogger();

            Log.Information("This should silently work, even though the connection string points to invalid endpoints");

            Assert.True(true);
        }

        [Fact]
        public void WhenALoggerUsesAnInvalidStorageConnectionStringItDoesntFail()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorage("InvalidConnectionString!!!")
                .CreateLogger();

            Log.Information("This should silently work, even though the connection string is malformed");

            Assert.True(true);
        }

        [Fact]
        public void WhenALoggerWithPropertiesUsesAnInvalidStorageConnectionStringItDoesntFail()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties("InvalidConnectionString!!!")
                .CreateLogger();

            Log.Information("This should silently work, even though the connection string is malformed");

            Assert.True(true);
        }

        private string GetAccountSharedAccessSignature(TableServiceClient storageAccount)
        {
            var sasBuilder = storageAccount.GetSasBuilder(
                "rwlau",
                TableAccountSasResourceTypes.All,
                DateTimeOffset.UtcNow.AddHours(24));
            var sasUrl = storageAccount.GenerateSasUri(sasBuilder);
            return sasUrl.Query.Substring(1);
        }

        [Fact(Skip = "Does not work with Storage Emulator, HTTPS table endpoint is required when SAS token is used")]
        public async Task WhenALoggerUsesASASSinkItIsRetrievableFromTheTableWithProperties()
        {
            string connectionString = DevelopmentStorageAccountConnectionString;
            string tableEndpoint = DevelopmentStorageAccountTableEndpoint;
            var storageAccount = new TableServiceClient(connectionString);
            var table = storageAccount.GetTableClient("LogEventEntity");

            await table.DeleteAsync();
            await table.CreateIfNotExistsAsync();

            var sharedAccessSignature = GetAccountSharedAccessSignature(storageAccount);

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(
                    sharedAccessSignature,
                    "test",
                    new Uri(tableEndpoint))
                .CreateLogger();

            var exception = new ArgumentException("Some exception");

            const string messageTemplate = "{Properties} should go in their {Numbered} {Space}";

            logger.Information(exception, messageTemplate, "Properties", 1234, ' ');

            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            // Check the presence of same properties as in previous version
            Assert.Equal(messageTemplate, result["MessageTemplate"]);
            Assert.Equal("Information", result["Level"]);
            Assert.Equal("System.ArgumentException: Some exception", result["Exception"]);
            Assert.Equal("\"Properties\" should go in their 1234  ", result["RenderedMessage"]);

            // Check the presence of the new properties.
            Assert.Equal("Properties", result["Properties"]);
            Assert.Equal(1234, result["Numbered"]);
            Assert.Equal(" ", result["Space"]);
        }
    }
}
