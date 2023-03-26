using System.Linq;

using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace Serilog.Sinks;
internal static class AzureTableStorageExtensions
{
    /// <summary>
    /// Synchronously creates a table without throwing a hidden expcetion when it already exists
    /// </summary>
    /// <param name="tableServiceClient">Authenticated TableServiceClient</param>
    /// <param name="table">The table name</param>
    /// <returns>Azure Response, null if table already existed</returns>
    public static Response<TableItem> CreateTableIfNotExistsWithout409(this TableServiceClient tableServiceClient, string table)
    {
        var tables = tableServiceClient.Query(x => x.Name == table).ToList();
        if (!tables.Any())
        {
            return tableServiceClient.CreateTable(table);
        }
        return null;
    }
}
