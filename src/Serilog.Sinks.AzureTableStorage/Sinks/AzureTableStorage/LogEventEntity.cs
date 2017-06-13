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
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.AzureTableStorage
{
    // todo: Figure out a better name than LogEventEntity given the table name is the same and it is weird...
    /// <summary>
    /// Represents a single log event for the Serilog Azure Table Storage Sink.
    /// </summary>
    public class LogEventEntity : TableEntity
    {
        /// <summary>
        /// Default constructor for the Storage Client library to re-hydrate entities when querying.
        /// </summary>
        public LogEventEntity() { }

        /// <summary>
        /// Create a log event entity from a Serilog <see cref="LogEvent"/>.
        /// </summary>
        /// <param name="log">The event to log</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="partitionKey">partition key to store</param>
        /// <param name="rowKey">row key to store</param>
        /// <param name="textFormatter">The text formatter to format the data</param>
        public LogEventEntity(
            LogEvent log,
            IFormatProvider formatProvider,
            string partitionKey,
            string rowKey,
            ITextFormatter textFormatter)
        {
            Timestamp = log.Timestamp.ToUniversalTime().DateTime;
            PartitionKey = partitionKey;
            RowKey = GetValidRowKey(rowKey);
            MessageTemplate = log.MessageTemplate.Text;
            Level = log.Level.ToString();
            Exception = log.Exception?.ToString();
            RenderedMessage = log.RenderMessage(formatProvider);
            var s = new StringWriter();
            if (textFormatter == null)
            {
                textFormatter = new JsonFormatter(closingDelimiter: "", formatProvider: formatProvider);
            }
            textFormatter.Format(log, s);
            Data = s.ToString();
        }

        // http://msdn.microsoft.com/en-us/library/windowsazure/dd179338.aspx
        static string GetValidRowKey(string rowKey)
        {
            rowKey = ObjectNaming.KeyFieldValueCharactersNotAllowedMatch.Replace(rowKey, "");
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
        /// A text-serialised representation of the data attached to the log message.
        /// </summary>
        public string Data { get; set; }
    }
}
