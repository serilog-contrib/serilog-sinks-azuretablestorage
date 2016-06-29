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

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Table Storage table.
    /// </summary>
    public class AzureTableStorageSink : ILogEventSink
    {
        readonly IFormatProvider _formatProvider;
        readonly CloudTable _table;
        long _rowKeyIndex;

        Action<ITableEntity> _rowKeyFunc;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="rowKeyFunc">Function to modify the generated row key</param>
        public AzureTableStorageSink(CloudStorageAccount storageAccount, IFormatProvider formatProvider, string storageTableName = null, Action<ITableEntity> rowKeyFunc = null)
        {
            _formatProvider = formatProvider;
            var tableClient = storageAccount.CreateCloudTableClient();

            if (string.IsNullOrEmpty(storageTableName))
            {
                storageTableName = typeof(LogEventEntity).Name;
            }

            _table = tableClient.GetTableReference(storageTableName);
            _table.CreateIfNotExists();

            _rowKeyFunc = rowKeyFunc ?? EnsureUniqueRowKey;
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
                logEvent.Timestamp.ToUniversalTime().Ticks);

            EnsureUniqueRowKey(logEventEntity);

            if (_rowKeyFunc != null)
                _rowKeyFunc(logEventEntity);

            _table.Execute(TableOperation.Insert(logEventEntity));
        }

        /// <summary>
        /// Appends an incrementing index to the row key to ensure that it will
        /// not conflict with existing rows created at the same time / with the
        /// same partition key.
        /// </summary>
        /// <param name="logEventEntity"></param>
        void EnsureUniqueRowKey(ITableEntity logEventEntity)
        {
            logEventEntity.RowKey += "|" + Interlocked.Increment(ref _rowKeyIndex);
        }
    }
}