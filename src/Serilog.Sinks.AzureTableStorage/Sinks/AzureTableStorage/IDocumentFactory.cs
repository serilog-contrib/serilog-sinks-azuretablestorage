using Azure.Data.Tables;

using Serilog.Events;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// 
/// </summary>
public interface IDocumentFactory
{
    /// <summary>
    /// Creates a <see cref="TableEntity"/> from specified <see cref="LogEvent"/>.
    /// </summary>
    /// <param name="logEvent">The log event.</param>
    /// <returns>An instance of <see cref="TableEntity"/></returns>
    TableEntity Create(LogEvent logEvent);
}
