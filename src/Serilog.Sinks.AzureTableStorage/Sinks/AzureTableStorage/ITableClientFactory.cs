using System;
using System.Collections.Generic;
using System.Text;

using Azure.Data.Tables;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Interface for creating <see cref="TableClient"/> instances
/// </summary>
public interface ITableClientFactory
{
    /// <summary>
    /// Creates <see cref="TableClient"/> instance with the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The sink options.</param>
    /// <param name="tableServiceClient">The table service client.</param>
    /// <returns>An insteance of <see cref="TableClient"/></returns>
    TableClient CreateTableClient(AzureTableStorageSinkOptions options, TableServiceClient tableServiceClient);
}
