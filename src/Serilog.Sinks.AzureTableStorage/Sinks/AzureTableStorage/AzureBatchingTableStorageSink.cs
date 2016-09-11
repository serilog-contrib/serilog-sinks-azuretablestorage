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
using System.Collections.Generic;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System.Threading.Tasks;

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Table Storage table.
    /// </summary>
    public class AzureBatchingTableStorageSink : PeriodicBatchingSink
    {
        readonly int _waitTimeoutMilliseconds = Timeout.Infinite;
        readonly IFormatProvider _formatProvider;
        readonly CloudTable _table;

        long _partitionKey;
        int _batchRowId;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="batchSizeLimit"></param>
        /// <param name="period"></param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        public AzureBatchingTableStorageSink(CloudStorageAccount storageAccount, IFormatProvider formatProvider, int batchSizeLimit, TimeSpan period, string storageTableName = null)
            :base(batchSizeLimit, period)
        {
            if (batchSizeLimit < 1 || batchSizeLimit > 100)
                throw new ArgumentException("batchSizeLimit must be between 1 and 100 for Azure Table Storage");
            _formatProvider = formatProvider;
            var tableClient = storageAccount.CreateCloudTableClient();

            if (string.IsNullOrEmpty(storageTableName)) storageTableName = typeof(LogEventEntity).Name;

            _table = tableClient.GetTableReference(storageTableName);
            _table.CreateIfNotExistsAsync().Wait(_waitTimeoutMilliseconds);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var operation = new TableBatchOperation();

            var first = true;

            foreach (var logEvent in events)
            {
                if (first)
                {
                    //check to make sure the partition key is not the same as the previous batch
                    var ticks = logEvent.Timestamp.ToUniversalTime().Ticks;
                    if (_partitionKey != ticks)
                    {
                        _batchRowId = 0; //the partitionkey has been reset
                        _partitionKey = ticks; //store the new partition key
                    }
                    first = false;
                }

                var logEventEntity = new LogEventEntity(logEvent, _formatProvider, _partitionKey);
                logEventEntity.RowKey += "|" + _batchRowId;
                operation.Add(TableOperation.Insert(logEventEntity));

                _batchRowId++;
            }

            await _table.ExecuteBatchAsync(operation);
        }

    }
}
