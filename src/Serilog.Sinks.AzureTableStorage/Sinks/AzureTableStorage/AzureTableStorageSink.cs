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
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Table Storage table.
    /// </summary>
    public class AzureTableStorageSink : ILogEventSink
    {
        readonly int _waitTimeoutMilliseconds = Timeout.Infinite;
        readonly IFormatProvider _formatProvider;
        readonly IKeyGenerator _keyGenerator;
        readonly ITextFormatter _textFormatter;
        readonly CloudTable _table;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="keyGenerator">generator used to generate partition keys and row keys</param>
        /// <param name="textFormatter">The text formatter to format the data</param>
        public AzureTableStorageSink(
            CloudStorageAccount storageAccount,
            IFormatProvider formatProvider,
            string storageTableName = null,
            IKeyGenerator keyGenerator = null,
            ITextFormatter textFormatter = null)
        {
            _formatProvider = formatProvider;
            _keyGenerator = keyGenerator ?? new DefaultKeyGenerator();
            _textFormatter = textFormatter;
            var tableClient = storageAccount.CreateCloudTableClient();

            if (string.IsNullOrEmpty(storageTableName))
            {
                storageTableName = typeof(LogEventEntity).Name;
            }

            _table = tableClient.GetTableReference(storageTableName);
            _table.CreateIfNotExistsAsync().SyncContextSafeWait(_waitTimeoutMilliseconds);
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            var logEventEntity = new LogEventEntity(
                logEvent,
                _formatProvider,
                _keyGenerator.GeneratePartitionKey(logEvent),
                _keyGenerator.GenerateRowKey(logEvent),
                _textFormatter
                );

            _table.ExecuteAsync(TableOperation.Insert(logEventEntity))
                .SyncContextSafeWait(_waitTimeoutMilliseconds);
        }
    }
}
