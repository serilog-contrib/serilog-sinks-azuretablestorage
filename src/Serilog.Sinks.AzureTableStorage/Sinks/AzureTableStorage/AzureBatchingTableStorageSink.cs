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

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.AzureTableProvider;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Threading;
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
        readonly IKeyGenerator _keyGenerator;
        readonly CloudStorageAccount _storageAccount;
        readonly ICloudTableProvider _cloudTableProvider;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="batchSizeLimit"></param>
        /// <param name="period"></param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="rollOnDateChange">Roll on to create new table on date change.</param>
        public AzureBatchingTableStorageSink(
            CloudStorageAccount storageAccount,
            IFormatProvider formatProvider,
            int batchSizeLimit,
            TimeSpan period,
            string storageTableName = null,
            bool rollOnDateChange = false)
            : this(storageAccount, formatProvider, batchSizeLimit, period, storageTableName, new DefaultKeyGenerator(), rollOnDateChange)
        {
        }

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="batchSizeLimit"></param>
        /// <param name="period"></param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="keyGenerator">generator used for partition keys and row keys</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        public AzureBatchingTableStorageSink(
            CloudStorageAccount storageAccount,
            IFormatProvider formatProvider,
            int batchSizeLimit,
            TimeSpan period,
            string storageTableName = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false,
            ICloudTableProvider cloudTableProvider = null)
            : base(batchSizeLimit, period)
        {
            if (batchSizeLimit < 1 || batchSizeLimit > 100)
                throw new ArgumentException("batchSizeLimit must be between 1 and 100 for Azure Table Storage");

            _formatProvider = formatProvider;
            _keyGenerator = keyGenerator ?? new DefaultKeyGenerator();

            if (string.IsNullOrEmpty(storageTableName))
            {
                storageTableName = typeof(LogEventEntity).Name;
            }

            _storageAccount = storageAccount;
            _cloudTableProvider = cloudTableProvider ?? new DefaultCloudTableProvider(storageTableName, bypassTableCreationValidation);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var table = _cloudTableProvider.GetCloudTable(_storageAccount);
            TableBatchOperation operation = new TableBatchOperation();

            string lastPartitionKey = null;

            foreach (var logEvent in events)
            {
                var partitionKey = _keyGenerator.GeneratePartitionKey(logEvent);

                if (partitionKey != lastPartitionKey)
                {
                    lastPartitionKey = partitionKey;
                    if (operation.Count > 0)
                    {
                        await table.ExecuteBatchAsync(operation).ConfigureAwait(false);
                        operation = new TableBatchOperation();
                    }
                }
                var logEventEntity = new LogEventEntity(
                    logEvent,
                    _formatProvider,
                    partitionKey,
                    _keyGenerator.GenerateRowKey(logEvent)
                    );
                operation.Add(TableOperation.Insert(logEventEntity));
            }
            if (operation.Count > 0)
            {
                await table.ExecuteBatchAsync(operation).ConfigureAwait(false);
            }
        }
    }
}
