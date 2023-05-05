using System;
using System.Collections.Generic;

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Configuration options for the Azure Table Storage Sink
/// </summary>
public class AzureTableStorageSinkOptions
{
    /// <summary>
    /// Gets or sets the <see cref="LogEvent"/> text formatter.  Default to <see cref="JsonFormatter"/>
    /// </summary>
    /// <value>
    /// The <see cref="LogEvent"/> text formatter.
    /// </value>
    /// <remarks>
    /// The <see cref="LogEvent"/> is formatted to text and stored in the Data column.
    /// </remarks>
    public ITextFormatter Formatter { get; set; } = new JsonFormatter(closingDelimiter: "");

    ///<summary>
    /// Supplies culture-specific formatting information, or null.
    /// </summary>
    public IFormatProvider FormatProvider { get; set; }

    /// <summary>
    /// Gets or sets the name of the storage table. Defaults to LogEvent.
    /// </summary>
    /// <value>
    /// The name of the storage table.
    /// </value>
    public string StorageTableName { get; set; } = "LogEvent";

    /// <summary>
    /// Gets or sets a value indicating whether to bypass table creation validation.
    /// </summary>
    /// <value>
    ///   <c>true</c> to bypass table creation validation; otherwise, <c>false</c>.
    /// </value>
    public bool BypassTableCreationValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets the properties to be written to table columns. By default, all properties will be written to columns.
    /// </summary>
    /// <value>
    /// The properties to be written to table columns.
    /// </value>
    public HashSet<string> PropertyColumns { get; set; } = new();

    /// <summary>
    /// Gets or sets the partition key rounding time span. Default 5 minutes
    /// </summary>
    /// <value>
    /// The partition key rounding time span.
    /// </value>
    public TimeSpan PartitionKeyRounding { get; set; } = TimeSpan.FromMinutes(5);
}
