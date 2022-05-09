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
