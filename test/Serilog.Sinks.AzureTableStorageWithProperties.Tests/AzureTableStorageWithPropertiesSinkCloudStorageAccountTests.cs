using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Sinks.AzureTableStorage.Tests
{
    [Collection("AzureStorageIntegrationCloudStorageAccountTests")]
    public class AzureTableStorageWithPropertiesSinkCloudStorageAccountTests
    {
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
        public async Task WhenALoggerWritesToTheSinkWithAWindowsNewlineInTheTemplateItIsRetrievable()
        {
            // Prompted from https://github.com/serilog/serilog-sinks-azuretablestorage/issues/10
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

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
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

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
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

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

            //Assert.Equal(bytearrayValue, result.Properties["ByteArray"].BinaryValue);
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

#if NET472
            Assert.Equal("Struct0 { Struct1Val: Struct1 { IntVal: 10, StringVal: \"ABCDE\" }, Struct2Val: Struct2 { DateTimeVal: 12/03/2014 17:37:12, DoubleVal: 3.14159265358979 } }", result.Properties["Struct0"].StringValue);
#else
            Assert.Equal("Struct0 { Struct1Val: Struct1 { IntVal: 10, StringVal: \"ABCDE\" }, Struct2Val: Struct2 { DateTimeVal: 12/03/2014 17:37:12, DoubleVal: 3.141592653589793 } }", result.Properties["Struct0"].StringValue);
#endif
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItAllowsStringFormatNumericPropertyNames()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var expectedResult = "Hello \"world\"";

            logger.Information("Hello {0}", "world");
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal(expectedResult, result.Properties["RenderedMessage"].StringValue);
        }

        [Fact]
        public async Task WhenALoggerWritesToTheSinkItAllowsNamedAndNumericPropertyNames()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();
            await table.CreateIfNotExistsAsync();

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(storageAccount)
                .CreateLogger();

            var name = "John Smith";
            var expectedResult = "Hello \"world\" this is \"John Smith\" 1234";

            logger.Information("Hello {0} this is {Name} {_1234}", "world", name, 1234);
            var result = (await TableQueryTakeDynamicAsync(table, takeCount: 1)).First();

            Assert.Equal(expectedResult, result.Properties["RenderedMessage"].StringValue);
            Assert.Equal(name, result.Properties["Name"].StringValue);
            Assert.Equal(1234, result.Properties["_1234"].Int32Value);
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
            await table.CreateIfNotExistsAsync();

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

        private const string PolicyName = "MyPolicy";

        private async Task SetupTableStoredAccessPolicyAsync(CloudTable table)
        {
            var permissions = await table.GetPermissionsAsync();
            var policy = new SharedAccessTablePolicy();

            if (permissions.SharedAccessPolicies.Count > 0 && permissions.SharedAccessPolicies.ContainsKey(PolicyName))
            {
                // extend the existing one by 1h
                policy = permissions.SharedAccessPolicies[PolicyName];
                policy.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(48);
            }
            else
            {
                // create a new one
                policy = new SharedAccessTablePolicy()
                {
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(48),
                    Permissions = SharedAccessTablePermissions.Add | SharedAccessTablePermissions.Update
                };
                permissions.SharedAccessPolicies.Add(PolicyName, policy);
            }

            await table.SetPermissionsAsync(permissions);
        }

        private async Task<string> GetSASUrlForTableAsync(CloudTable table)
        {
            await SetupTableStoredAccessPolicyAsync(table);

            var permissions = await table.GetPermissionsAsync().ConfigureAwait(false);
            var policy = permissions.SharedAccessPolicies[PolicyName];

            var sasUrl = table.GetSharedAccessSignature(null, PolicyName);

            return sasUrl;
        }

        [Fact]
        public async Task WhenALoggerUsesASASSinkItIsRetrievableFromTheTableWithProperties()
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("LogEventEntity");

            await table.DeleteIfExistsAsync();
            await table.CreateIfNotExistsAsync();

            var sasUrl = await GetSASUrlForTableAsync(table);

            var logger = new LoggerConfiguration()
                .WriteTo.AzureTableStorageWithProperties(sasUrl, "test", storageAccount.TableEndpoint)
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
    }
}
