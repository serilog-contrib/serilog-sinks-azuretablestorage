using System;
using System.IO;
using System.Text.RegularExpressions;

using Azure.Data.Tables;

using Serilog.Events;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// 
/// </summary>
public class DefaultDocumentFactory : IDocumentFactory
{
    // Azure tables support a maximum of 255 columns.
    private const int _maxDocumentColumns = 255;

    private readonly AzureTableStorageSinkOptions _sinkOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDocumentFactory"/> class.
    /// </summary>
    /// <param name="sinkOptions">The sink options.</param>
    public DefaultDocumentFactory(AzureTableStorageSinkOptions sinkOptions)
    {
        _sinkOptions = sinkOptions ?? throw new ArgumentNullException(nameof(sinkOptions));
    }

    /// <summary>
    /// Creates a <see cref="TableEntity"/> from specified <see cref="LogEvent"/>.
    /// </summary>
    /// <param name="logEvent">The log event.</param>
    /// <returns>An instance of <see cref="TableEntity"/></returns>
    public virtual TableEntity Create(LogEvent logEvent)
    {
        var keyGenerator = _sinkOptions.KeyGenerator ?? new DefaultKeyGenerator();
        var tableEntity = new TableEntity
        {
            PartitionKey = keyGenerator.GeneratePartitionKey(logEvent),
            RowKey = keyGenerator.GenerateRowKey(logEvent),
            Timestamp = logEvent.Timestamp
        };

        tableEntity["Level"] = logEvent.Level.ToString();
        tableEntity["MessageTemplate"] = logEvent.MessageTemplate.Text;
        tableEntity["RenderedMessage"] = logEvent.RenderMessage(_sinkOptions.FormatProvider);

        if (logEvent.Exception != null)
            tableEntity["Exception"] = logEvent.Exception.ToString();

        using var writer = new StringWriter();

        var formatter = _sinkOptions.Formatter ?? new JsonFormatter(closingDelimiter: "");
        formatter.Format(logEvent, writer);

        tableEntity["Data"] = writer.ToString();

        var count = tableEntity.Count;

        foreach (var logProperty in logEvent.Properties)
        {
            var propertyKey = logProperty.Key;
            var isValid = IsValidColumnName(propertyKey) && ShouldIncludeProperty(propertyKey);

            if (!isValid || count++ >= _maxDocumentColumns - 1)
                continue;

            var propertyValue = ConvertValue(logProperty.Value, null, _sinkOptions.FormatProvider);
            tableEntity[propertyKey] = propertyValue;
        }

        return tableEntity;
    }

    /// <summary>
    /// Determines whether the specified property name is valid column name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>
    ///   <c>true</c> if the property name is valid column name; otherwise, <c>false</c>.
    /// </returns>
    protected bool IsValidColumnName(string propertyName)
    {
        const string regex = @"^(?:((?!\d)\w+(?:\.(?!\d)\w+)*)\.)?((?!\d)\w+)$";
        return Regex.Match(propertyName, regex).Success;
    }

    /// <summary>
    /// Determines whether the specified property name should be included.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>
    ///   <c>true</c> if the property name should be included; otherwise, <c>false</c>.
    /// </returns>
    protected bool ShouldIncludeProperty(string propertyName)
    {
        return _sinkOptions.PropertyColumns == null
               || _sinkOptions.PropertyColumns.Count == 0
               || _sinkOptions.PropertyColumns.Contains(propertyName);
    }

    /// <summary>
    /// Converts the specified log value to an object.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns></returns>
    protected object ConvertValue(LogEventPropertyValue value, string format = null, IFormatProvider formatProvider = null)
    {
        return value switch
        {
            ScalarValue scalarValue => SimplifyScalar(scalarValue.Value),
            DictionaryValue dictionaryValue => dictionaryValue.ToString(format, formatProvider),
            SequenceValue sequenceValue => sequenceValue.ToString(format, formatProvider),
            StructureValue structureValue => structureValue.ToString(format, formatProvider),
            _ => null
        };
    }

    private static object SimplifyScalar(object value)
    {
        return value switch
        {
            byte[] bytesValue => bytesValue,
            bool boolValue => boolValue,
            DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue,
            DateTime dateTimeValue => dateTimeValue,
            double doubleValue => doubleValue,
            Guid guidValue => guidValue,
            int intValue => intValue,
            long longValue => longValue,
            string stringValue => stringValue,
            _ => value?.ToString()
        };
    }

}
