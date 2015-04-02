### Serilog.Sinks.AzureTableStorage

[![Build status](https://ci.appveyor.com/api/projects/status/bb9v4y9dguyn7w9a/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-azuretablestorage/branch/master)

Writes to a table in [[Windows Azure Table Storage|http://www.windowsazure.com/en-us/develop/net/how-to-guides/table-services/]].

**Package** - [[Serilog.AzureTableStorage|http://nuget.org/packages/serilog.sinks.azuretablestorage]]
| **Platforms** - .NET 4.5

```csharp
var storage = CloudStorageAccount.FromConfigurationSetting("MyStorage");

var log = new LoggerConfiguration()
    .WriteTo.AzureTableStorage(storage)
    .CreateLogger();
```
