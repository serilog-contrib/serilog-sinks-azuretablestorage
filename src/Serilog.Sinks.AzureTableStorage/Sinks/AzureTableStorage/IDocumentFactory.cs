using Azure.Data.Tables;

using Serilog.Events;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Interface defining Azure Table Storage document factory
/// </summary>
public interface IDocumentFactory
{
    /// <summary>
    /// Creates a <see cref="TableEntity"/> from specified <see cref="LogEvent"/>.
    /// </summary>
    /// <param name="logEvent">The log event.</param>
    /// <param name="options">The table storage options.</param>
    /// <param name="keyGenerator">The document key generator.</param>
    /// <returns>An instance of <see cref="TableEntity"/></returns>
    TableEntity Create(LogEvent logEvent, AzureTableStorageSinkOptions options, IKeyGenerator keyGenerator);
}
