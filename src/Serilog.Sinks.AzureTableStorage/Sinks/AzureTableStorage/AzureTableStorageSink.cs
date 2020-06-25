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
using Microsoft.Azure.Cosmos.Table;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.AzureTableStorage.AzureTableProvider;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;

namespace Serilog.Sinks.AzureTableStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Table Storage table.
    /// </summary>
    public class AzureTableStorageSink : ILogEventSink
    {
        readonly int _waitTimeoutMilliseconds = Timeout.Infinite;
        readonly ITextFormatter _textFormatter;
        readonly IKeyGenerator _keyGenerator;
        readonly CloudStorageAccount _storageAccount;
        readonly string _storageTableName;
        readonly bool _bypassTableCreationValidation;
        readonly ICloudTableProvider _cloudTableProvider;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="textFormatter"></param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="keyGenerator">generator used to generate partition keys and row keys</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        public AzureTableStorageSink(
            CloudStorageAccount storageAccount,
            ITextFormatter textFormatter,
            string storageTableName = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false,
            ICloudTableProvider cloudTableProvider = null)
        {
            _textFormatter = textFormatter;
            _keyGenerator = keyGenerator ?? new DefaultKeyGenerator();

            if (string.IsNullOrEmpty(storageTableName))
            {
                storageTableName = typeof(LogEventEntity).Name;
            }

            _storageAccount = storageAccount;
            _storageTableName = storageTableName;
            _bypassTableCreationValidation = bypassTableCreationValidation;
            _cloudTableProvider = cloudTableProvider ?? new DefaultCloudTableProvider();
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            var table = _cloudTableProvider.GetCloudTable(_storageAccount, _storageTableName, _bypassTableCreationValidation);
            var logEventEntity = new LogEventEntity(
                logEvent,
                _textFormatter,
                _keyGenerator.GeneratePartitionKey(logEvent),
                _keyGenerator.GenerateRowKey(logEvent)
                );

            table.ExecuteAsync(TableOperation.InsertOrMerge(logEventEntity))
                .SyncContextSafeWait(_waitTimeoutMilliseconds);
        }
    }
}

