using Serilog.Events;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Interface used to generate row keys for <see cref="LogEvent"/>s
/// </summary>
public interface IKeyGenerator
{
    /// <summary>
    /// Generate the partition key based on the supplied <paramref name="logEvent"/>
    /// </summary>
    /// <param name="logEvent">the log event</param>
    /// <param name="options">The table storage options.</param>
    /// <returns>The partition key for the Azure table</returns>
    string GeneratePartitionKey(LogEvent logEvent, AzureTableStorageSinkOptions options);

    /// <summary>
    /// Generate a row key for the supplied <paramref name="logEvent"/>.
    /// </summary>
    /// <param name="logEvent">the log event</param>
    /// <param name="options">The table storage options.</param>
    /// <returns>The row key for the Azure table</returns>
    string GenerateRowKey(LogEvent logEvent, AzureTableStorageSinkOptions options);
}
