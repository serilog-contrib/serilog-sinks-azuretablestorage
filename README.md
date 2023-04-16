# Serilog.Sinks.AzureTableStorage [![Nuget](https://img.shields.io/nuget/v/serilog.sinks.azuretablestorage)](http://nuget.org/packages/serilog.sinks.azuretablestorage)
Writes to a table in [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/).

**Package** - [Serilog.Sinks.AzureTableStorage](http://nuget.org/packages/serilog.sinks.azuretablestorage) | **Platforms** - .NET Standard 2.0

```csharp
var log = new LoggerConfiguration()
    .WriteTo.AzureTableStorage("<connectionString>")
    .CreateLogger();
```

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

### Async approach
It is possible to configure the collector using [Serilog.Sinks.Async](https://github.com/serilog/serilog-sinks-async) to have an async approach.

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
