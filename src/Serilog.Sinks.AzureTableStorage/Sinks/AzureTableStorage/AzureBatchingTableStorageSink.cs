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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Table Storage table.
    /// </summary>
    public class AzureBatchingTableStorageSink : PeriodicBatchingSink
    {
        private readonly int _waitTimeoutMilliseconds = Timeout.Infinite;
        private readonly IFormatProvider _formatProvider;
        private readonly IKeyGenerator _keyGenerator;
        private readonly CloudTable _table;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="batchSizeLimit"></param>
        /// <param name="period"></param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        public AzureBatchingTableStorageSink(
            CloudStorageAccount storageAccount,
            IFormatProvider formatProvider,
            int batchSizeLimit,
            TimeSpan period,
            string storageTableName = null)
            : this(storageAccount, formatProvider, batchSizeLimit, period, storageTableName, new DefaultKeyGenerator())
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
        public AzureBatchingTableStorageSink(
            CloudStorageAccount storageAccount,
            IFormatProvider formatProvider,
            int batchSizeLimit,
            TimeSpan period,
            string storageTableName = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false)
            : base(batchSizeLimit, period)
        {
            if (batchSizeLimit < 1 || batchSizeLimit > 100)
                throw new ArgumentException("batchSizeLimit must be between 1 and 100 for Azure Table Storage");

            _formatProvider = formatProvider;
            _keyGenerator = keyGenerator ?? new DefaultKeyGenerator();
            var tableClient = storageAccount.CreateCloudTableClient();

            if (string.IsNullOrEmpty(storageTableName)) storageTableName = typeof(LogEventEntity).Name;

            _table = tableClient.GetTableReference(storageTableName);

            // In some cases (e.g.: SAS URI), we might not have enough permissions to create the table if
            // it does not already exists. So, if we are in that case, we ignore the error as per bypassTableCreationValidation.
            try
            {
                _table.CreateIfNotExistsAsync().SyncContextSafeWait(_waitTimeoutMilliseconds);
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine($"Failed to create table: {ex}");
                if (!bypassTableCreationValidation)
                {
                    throw;
                }
            }
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
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
                        await _table.ExecuteBatchAsync(operation).ConfigureAwait(false);
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
                await _table.ExecuteBatchAsync(operation).ConfigureAwait(false);
            }
        }
    }
}
