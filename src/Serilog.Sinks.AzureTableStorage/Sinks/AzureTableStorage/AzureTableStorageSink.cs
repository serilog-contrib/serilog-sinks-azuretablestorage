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
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Table Storage table.
    /// </summary>
    public class AzureTableStorageSink : ILogEventSink
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
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="keyGenerator">generator used to generate partition keys and row keys</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        public AzureTableStorageSink(
            CloudStorageAccount storageAccount,
            IFormatProvider formatProvider,
            string storageTableName = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false)
        {
            _formatProvider = formatProvider;
            _keyGenerator = keyGenerator ?? new DefaultKeyGenerator();
            var tableClient = storageAccount.CreateCloudTableClient();

            if (string.IsNullOrEmpty(storageTableName))
            {
                storageTableName = typeof(LogEventEntity).Name;
            }

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
                _keyGenerator.GenerateRowKey(logEvent)
                );

            _table.ExecuteAsync(TableOperation.Insert(logEventEntity))
                .SyncContextSafeWait(_waitTimeoutMilliseconds);
        }
    }
}
