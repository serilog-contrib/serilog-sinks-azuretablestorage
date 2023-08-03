using System;

using Azure.Data.Tables;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Default <see cref="ITableClientFactory"/> implementation
/// </summary>
public class DefaultTableClientFactory : ITableClientFactory
{
    private TableClient _tableClient = null;

    /// <summary>
    /// Creates <see cref="TableClient" /> instance with the specified <paramref name="options" />.
    /// </summary>
    /// <param name="options">The sink options.</param>
    /// <param name="tableServiceClient">The table service client.</param>
    /// <returns>
    /// An insteance of <see cref="TableClient" />
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// if <paramref name="options"/> or <paramref name="tableServiceClient"/> is null
    /// </exception>
    public TableClient CreateTableClient(AzureTableStorageSinkOptions options, TableServiceClient tableServiceClient)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (tableServiceClient is null)
            throw new ArgumentNullException(nameof(tableServiceClient));


        if (_tableClient != null)
            return _tableClient;

        var tableName = options.StorageTableName ?? "LogEvent";
        _tableClient = tableServiceClient.GetTableClient(tableName);

        try
        {
            _tableClient.CreateIfNotExists();
        }
        catch (Exception ex)
        {
            Debugging.SelfLog.WriteLine($"Failed to create table: {ex}");
            if (options.BypassTableCreationValidation)
                return _tableClient;

            throw;
        }

        return _tableClient;
    }
}
