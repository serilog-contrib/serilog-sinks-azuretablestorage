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

using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.AzureTableProvider;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;
using Serilog.Sinks.AzureTableStorage.Sinks.KeyGenerator;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Table Storage table.
    /// </summary>
    public class AzureBatchingTableStorageWithPropertiesSink : PeriodicBatchingSink
    {
        readonly IFormatProvider _formatProvider;
        readonly string _additionalRowKeyPostfix;
        readonly string[] _propertyColumns;
        const int _maxAzureOperationsPerBatch = 100;
        readonly IKeyGenerator _keyGenerator;
        readonly TableServiceClient _storageAccount;
        readonly string _storageTableName;
        readonly bool _bypassTableCreationValidation;
        readonly ICloudTableProvider _cloudTableProvider;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="batchSizeLimit"></param>
        /// <param name="period"></param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="additionalRowKeyPostfix">Additional postfix string that will be appended to row keys</param>
        /// <param name="keyGenerator">Generates the PartitionKey and the RowKey</param>
        /// <param name="propertyColumns">Specific properties to be written to columns. By default, all properties will be written to columns.</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        public AzureBatchingTableStorageWithPropertiesSink(
            TableServiceClient storageAccount,
            IFormatProvider formatProvider,
            int batchSizeLimit,
            TimeSpan period,
            string storageTableName = null,
            string additionalRowKeyPostfix = null,
            IKeyGenerator keyGenerator = null,
            string[] propertyColumns = null,
            bool bypassTableCreationValidation = false,
            ICloudTableProvider cloudTableProvider = null)
            : base(batchSizeLimit, period)
        {
            if (string.IsNullOrEmpty(storageTableName))
            {
                storageTableName = "LogEventEntity";
            }

            _storageAccount = storageAccount;
            _storageTableName = storageTableName;
            _bypassTableCreationValidation = bypassTableCreationValidation;
            _cloudTableProvider = cloudTableProvider ?? new DefaultCloudTableProvider();

            _formatProvider = formatProvider;
            _additionalRowKeyPostfix = additionalRowKeyPostfix;
            _propertyColumns = propertyColumns;
            _keyGenerator = keyGenerator ?? new PropertiesKeyGenerator();
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var table = _cloudTableProvider.GetCloudTable(_storageAccount, _storageTableName, _bypassTableCreationValidation);
            string lastPartitionKey = null;
            var transactionActions = new List<TableTransactionAction>();
            var insertsPerOperation = 0;

            foreach (var logEvent in events)
            {
                var tableEntity = AzureTableStorageEntityFactory.CreateEntityWithProperties(logEvent, _formatProvider, _additionalRowKeyPostfix, _keyGenerator, _propertyColumns);

                // If partition changed, store the new and force an execution
                if (lastPartitionKey != tableEntity.PartitionKey)
                {
                    lastPartitionKey = tableEntity.PartitionKey;

                    // Force a new execution
                    insertsPerOperation = _maxAzureOperationsPerBatch;
                }

                // If reached max operations per batch, we need a new batch operation
                if (insertsPerOperation == _maxAzureOperationsPerBatch)
                {
                    // If there is an operation currently in use, execute it
                    if (transactionActions.Count > 0)
                    {
                        await table.SubmitTransactionAsync(transactionActions).ConfigureAwait(false);
                    }

                    // Create a new batch operation and zero count
                    transactionActions = new List<TableTransactionAction>();
                    insertsPerOperation = 0;
                }

                // Add current entry to the batch
                var transactionAction =
                    new TableTransactionAction(TableTransactionActionType.UpdateMerge, tableEntity);
                transactionActions.Add(transactionAction);

                insertsPerOperation++;
            }

            // Execute last batch
            if (transactionActions.Count > 0)
            {
                await table.SubmitTransactionAsync(transactionActions).ConfigureAwait(false);
            }
        }
    }
}
