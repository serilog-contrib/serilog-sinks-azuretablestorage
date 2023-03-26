// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;

using Azure;
using Azure.Data.Tables;

using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AzureTableStorage;

// todo: Figure out a better name than LogEventEntity given the table name is the same and it is weird...
/// <summary>
/// Represents a single log event for the Serilog Azure Table Storage Sink.
/// </summary>
public class LogEventEntity : ITableEntity
{
    /// <summary>
    /// Default constructor for the Storage Client library to re-hydrate entities when querying.
    /// </summary>
    public LogEventEntity() { }

    /// <summary>
    /// Create a log event entity from a Serilog <see cref="LogEvent"/>.
    /// </summary>
    /// <param name="log">The event to log</param>
    /// <param name="textFormatter"></param>
    /// <param name="partitionKey">partition key to store</param>
    /// <param name="rowKey">row key to store</param>
    public LogEventEntity(
        LogEvent log,
        ITextFormatter textFormatter,
        string partitionKey,
        string rowKey)
    {
        Timestamp = log.Timestamp.ToUniversalTime();
        PartitionKey = partitionKey;
        RowKey = GetValidRowKey(rowKey);
        MessageTemplate = log.MessageTemplate.Text;
        Level = log.Level.ToString();
        Exception = log.Exception?.ToString();
        RenderedMessage = log.RenderMessage();

        //Use the underlying TextFormatter to serialise the entire JSON object for the data column
        using (var s = new StringWriter())
        {
            textFormatter.Format(log, s);
            Data = s.ToString();
        }
    }

    // http://msdn.microsoft.com/en-us/library/windowsazure/dd179338.aspx
    /// <summary>
    /// Gets the valid row key.
    /// </summary>
    /// <param name="rowKey">The row key.</param>
    /// <returns></returns>
    public static string GetValidRowKey(string rowKey)
    {
        rowKey = ObjectNaming.KeyFieldValueCharactersNotAllowedMatch.Replace(rowKey, "");
        return rowKey.Length > 1024 ? rowKey.Substring(0, 1024) : rowKey;
    }

    /// <summary>
    ///   <c>ITableEntity</c> property implementations
    /// </summary>
    /// <value>
    /// A string containing the partition key for the entity.
    /// </value>
    public string PartitionKey { get; set; }

    /// <summary>
    /// The row key is a unique identifier for an entity within a given partition. Together the <see cref="P:Azure.Data.Tables.ITableEntity.PartitionKey" /> and RowKey uniquely identify every entity within a table.
    /// </summary>
    /// <value>
    /// A string containing the row key for the entity.
    /// </value>
    public string RowKey { get; set; }

    /// <summary>
    /// The Timestamp property is a DateTime value that is maintained on the server side to record the time an entity was last modified.
    /// The Table service uses the Timestamp property internally to provide optimistic concurrency. The value of Timestamp is a monotonically increasing value,
    /// meaning that each time the entity is modified, the value of Timestamp increases for that entity.
    /// This property should not be set on insert or update operations (the value will be ignored).
    /// </summary>
    /// <value>
    /// A <see cref="T:System.DateTimeOffset" /> containing the timestamp of the entity.
    /// </value>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the entity's ETag.
    /// </summary>
    /// <value>
    /// A string containing the ETag value for the entity.
    /// </value>
    public ETag ETag { get; set; }

    /// <summary>
    /// The template that was used for the log message.
    /// </summary>
    public string MessageTemplate { get; set; }
    /// <summary>
    /// The level of the log.
    /// </summary>
    public string Level { get; set; }
    /// <summary>
    /// A string representation of the exception that was attached to the log (if any).
    /// </summary>
    public string Exception { get; set; }
    /// <summary>
    /// The rendered log message.
    /// </summary>
    public string RenderedMessage { get; set; }
    /// <summary>
    /// A JSON-serialised representation of the data attached to the log message.
    /// </summary>
    public string Data { get; set; }
}
