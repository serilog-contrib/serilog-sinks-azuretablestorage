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
