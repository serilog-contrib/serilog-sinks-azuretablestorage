# Serilog.Sinks.AzureTableStorage [![Nuget](https://img.shields.io/nuget/v/serilog.sinks.azuretablestorage)](http://nuget.org/packages/serilog.sinks.azuretablestorage)
Writes to a table in [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/).

**Package** - [Serilog.Sinks.AzureTableStorage](http://nuget.org/packages/serilog.sinks.azuretablestorage) | **Platforms** - .NET Standard 2.0

```csharp
var log = new LoggerConfiguration()
    .WriteTo.AzureTableStorage("<connectionString>")
    .CreateLogger();
```

## Configuration

| Configuration                 | Description                                                                         | Default                   |
|-------------------------------|-------------------------------------------------------------------------------------|---------------------------|
| connectionString              | The Cloud Storage Account connection string                                         |                           |
| sharedAccessSignature         | The storage account/table SAS key                                                   |                           |
| accountName                   | The storage account name                                                            |                           |
| restrictedToMinimumLevel      | The minimum log event level required in order to write an event to the sink.        | Verbose                   |
| formatProvider                | Culture-specific formatting information                                             |                           |
| storageTableName              | Table name that log entries will be written to                                      | LogEvent                  |
| writeInBatches                | Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink        | true                      |
| batchPostingLimit             | The maximum number of events to post in a single batch                              | 100                       |
| period                        | The time to wait between checking for event batches                                 | 0:0:2                     |
| keyGenerator                  | The key generator used to create the PartitionKey and the RowKey for each log entry | DefaultKeyGenerator       |
| propertyColumns               | Specific log event properties to be written as table columns                        |                           |
| bypassTableCreationValidation | Bypass the exception in case the table creation fails                               | false                     |
| documentFactory               | Provider to create table document from LogEvent                                     | DefaultDocumentFactory    |
| tableClientFactory            | Provider to create table client                                                     | DefaultTableClientFactory |
| partitionKeyRounding          | Partition key rounding time span                                                    | 0:5:0                     |

### JSON configuration

It is possible to configure the sink using [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) by specifying the table name and connection string in `appsettings.json`:

```json
"Serilog": {
  "WriteTo": [
    {"Name": "AzureTableStorage", "Args": {"storageTableName": "", "connectionString": ""}}
  ]
}
```

JSON configuration must be enabled using `ReadFrom.Configuration()`; see the [documentation of the JSON configuration package](https://github.com/serilog/serilog-settings-configuration) for details.

### XML `<appSettings>` configuration

To use the file sink with the [Serilog.Settings.AppSettings](https://github.com/serilog/serilog-settings-appsettings) package, first install that package if you haven't already done so:

```powershell
Install-Package Serilog.Settings.AppSettings
```

Instead of configuring the logger in code, call `ReadFrom.AppSettings()`:

```csharp
var log = new LoggerConfiguration()
    .ReadFrom.AppSettings()
    .CreateLogger();
```

In your application's `App.config` or `Web.config` file, specify the file sink assembly and required path format under the `<appSettings>` node:

```xml
<configuration>
  <appSettings>
    <add key="serilog:using:AzureTableStorage" value="Serilog.Sinks.AzureTableStorage" />
    <add key="serilog:write-to:AzureTableStorage.connectionString" value="DefaultEndpointsProtocol=https;AccountName=ACCOUNT_NAME;AccountKey=KEY;EndpointSuffix=core.windows.net" />
    <add key="serilog:write-to:AzureTableStorage.formatter" value="Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact" />
  </appSettings>
</configuration>
```

### Example Configuration for ASP.NET

```c#
public static class Program
{
    private const string OutputTemplate = "{Timestamp:HH:mm:ss.fff} [{Level:u1}] {Message:lj}{NewLine}{Exception}";

    public static async Task<int> Main(string[] args)
    {
        // azure home directory
        var homeDirectory = Environment.GetEnvironmentVariable("HOME") ?? ".";
        var logDirectory = Path.Combine(homeDirectory, "LogFiles");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.File(
                path: $"{logDirectory}/boot.txt",
                rollingInterval: RollingInterval.Day,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: OutputTemplate,
                retainedFileCountLimit: 10
            )
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting web host");

            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            builder.Host
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ApplicationName", builder.Environment.ApplicationName)
                    .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName)
                    .WriteTo.Console(outputTemplate: OutputTemplate)
                    .WriteTo.File(
                        path: $"{logDirectory}/log.txt",
                        rollingInterval: RollingInterval.Day,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1),
                        outputTemplate: OutputTemplate,
                        retainedFileCountLimit: 10
                    )
                    .WriteTo.AzureTableStorage(
                        connectionString: context.Configuration.GetConnectionString("StorageAccount"),
                        propertyColumns: new[] { "SourceContext", "RequestId", "RequestPath", "ConnectionId", "ApplicationName", "EnvironmentName" }
                    )
                );

            ConfigureServices(builder);

            var app = builder.Build();

            ConfigureMiddleware(app);

            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
    }

    private static void ConfigureMiddleware(Microsoft.AspNetCore.Builder.WebApplication app)
    {
    }
}
```

### Change Log

10.0.0
  * Breaking: writeInBatches removed, all writes are now batched
  * Update: update to serilog 4.0
  * Remove: removed dependance on Serilog.Sinks.PeriodicBatching, use serilog 4.0 `IBatchedLogEventSink`

9.6.0
  * Fix: improve timezone support

9.5.0
  * Add: use ULID for rowkey for speed and efficiency

9.4.0
  * Fix: prevent duplicate rowkey

9.1.0
  * Add: Built-in trace and span id support

9.0.0
  * Breaking: Due to issue with creating provides from configuration
    * `IDocumentFactory.Create` add AzureTableStorageSinkOptions and IKeyGenerator arguments
    * `IKeyGenerator.GeneratePartitionKey` add AzureTableStorageSinkOptions argument
    * `IKeyGenerator.GenerateRowKey` add AzureTableStorageSinkOptions argument
  * Fix: DefaultDocumentFactory and DefaultKeyGenerator needed paramaterless contructor for use in configuration files
  * Add: ITableClientFactory to control TableClient creation  

8.5.0
  * Add option for partition key rounding 
  * Move IKeyGenertor from options
  * Add DateTime extension methods for partition key and row key
  * Add sample web project

8.0.0
  * Breaking: major refactor to simplify code base
    * Removed: AzureTableStorageWithProperties extension removed, use equivalent AzureTableStorage
    * Removed: ICloudTableProvider provider removed
    * Added: IDocumentFactory to allow control over table document
    * Change: PartitionKey and RowKey changed to new implementation

7.0.0
  * Update dependencies: repace Microsoft.Azure.Cosmos.Table with Azure.Data.Tables

6.0.0
  * Updated dependencies: replace deprecated package WindowsAzure.Storage with Microsoft.Azure.Cosmos.Table 1.0.8
  * Updated dependencies: Serilog 2.10.0

5.0.0
 * Migrated to new CSPROJ project system
 * Updated dependencies: WindowsAzure.Storage 8.6.0, Serilog 2.6.0, Serilog.Sinks.PeriodicBatching 2.1.1
 * Fix #36 - Allow using SAS URI for logging.

1.5
 * Moved from serilog/serilog
