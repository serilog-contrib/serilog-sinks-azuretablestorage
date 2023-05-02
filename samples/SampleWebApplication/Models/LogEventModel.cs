using Azure;
using Azure.Data.Tables;

namespace SampleWebApplication.Models;

public class LogEventModel : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string Level { get; set; } = string.Empty;

    public string? MessageTemplate { get; set; }

    public string RenderedMessage { get; set; } = string.Empty;

    public string? Exception { get; set; }

    public string? Data { get; set; }
}
