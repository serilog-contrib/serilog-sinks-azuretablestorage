﻿// Copyright 2014 Serilog Contributors
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
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.AzureTableStorage
{
    // todo: Figure out a better name than LogEventEntity given the table name is the same and it is weird...
    /// <summary>
    /// Represents a single log event for the Serilog Azure Table Storage Sink.
    /// </summary>
    /// <remarks>
    /// The PartitionKey is set to "0" followed by the ticks of the log event time (in UTC) as per what Azure Diagnostics logging has.
    /// The RowKey is set to "{Level}|{MessageTemplate}" to allow you to search for certain categories of log messages or indeed for a
    ///     specific log message quickly using the indexing in Azure Table Storage.
    /// </remarks>
    public class LogEventEntity : TableEntity
    {
        static readonly Regex RowKeyNotAllowedMatch = new Regex(@"(\\|/|#|\?|[\x00-\x1f]|[\x7f-\x9f])");

        /// <summary>
        /// Default constructor for the Storage Client library to re-hydrate entities when querying.
        /// </summary>
        public LogEventEntity() { }

        /// <summary>
        /// Create a log event entity from a Serilog <see cref="LogEvent"/>.
        /// </summary>
        /// <param name="log">The event to log</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="partitionKey"></param>
        public LogEventEntity(LogEvent log, IFormatProvider formatProvider, long partitionKey)
        {
            Timestamp = log.Timestamp.ToUniversalTime().DateTime;
            PartitionKey = string.Format("0{0}", partitionKey);
            RowKey = GetValidRowKey(string.Format("{0}|{1}", log.Level, log.MessageTemplate.Text));
            MessageTemplate = log.MessageTemplate.Text;
            Level = log.Level.ToString();
            Exception = log.Exception != null ? log.Exception.ToString() : null;
            RenderedMessage = log.RenderMessage(formatProvider);
            var s = new StringWriter();
            new JsonFormatter(closingDelimiter: "", formatProvider: formatProvider).Format(log, s);
            Data = s.ToString();
        }

        // http://msdn.microsoft.com/en-us/library/windowsazure/dd179338.aspx
        static string GetValidRowKey(string rowKey)
        {
            rowKey = RowKeyNotAllowedMatch.Replace(rowKey, "");
            return rowKey.Length > 1024 ? rowKey.Substring(0, 1024) : rowKey;
        }

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
}
