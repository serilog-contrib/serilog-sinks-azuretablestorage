using System;
using System.Collections.Concurrent;

using Azure.Data.Tables;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Default <see cref="ITableClientFactory"/> implementation
/// </summary>
public class DefaultTableClientFactory : ITableClientFactory
{
    private readonly ConcurrentDictionary<string, TableClient> _tableClients = new();

    /// <summary>
    /// Creates <see cref="TableClient" /> instance with the specified <paramref name="options" />.
    /// </summary>
    /// <param name="options">The sink options.</param>
    /// <param name="tableServiceClient">The table service client.</param>
    /// <returns>
    /// An insteance of <see cref="TableClient" />
    /// </returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public TableClient CreateTableClient(AzureTableStorageSinkOptions options, TableServiceClient tableServiceClient)
    {
        var tableName = options.StorageTableName ?? "LogEvent";

        return _tableClients.GetOrAdd(tableName, _ =>
        {
            var tableClient = tableServiceClient.GetTableClient(tableName);

            try
            {
                tableClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine($"Failed to create table: {ex}");
                if (options.BypassTableCreationValidation)
                    return tableClient;

                throw;
            }

            return tableClient;
        });
    }
}
