# Serilog.Sinks.AzureTableStorage [![Build status](https://ci.appveyor.com/api/projects/status/bb9v4y9dguyn7w9a/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-azuretablestorage/branch/master)

Writes to a table in [Windows Azure Table Storage](https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-tables/).

**Package** - [Serilog.Sinks.AzureTableStorage](http://nuget.org/packages/serilog.sinks.azuretablestorage) | **Platforms** - .NET 4.5

```csharp
var storage = CloudStorageAccount.FromConfigurationSetting("MyStorage");

var log = new LoggerConfiguration()
    .WriteTo.AzureTableStorage(storage)
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
