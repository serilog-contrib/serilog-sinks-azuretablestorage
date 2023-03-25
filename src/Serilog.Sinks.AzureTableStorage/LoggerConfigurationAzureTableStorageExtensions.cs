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
using Azure;
using Azure.Data.Tables;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.AzureTableStorage;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;
using Serilog.Sinks.AzureTableStorage.AzureTableProvider;
using Serilog.Formatting.Json;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.AzureTableStorage() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationAzureTableStorageExtensions
    {
        /// <summary>
        /// A reasonable default for the number of events posted in
        /// each batch.
        /// </summary>
        public const int DefaultBatchPostingLimit = 50;

        /// <summary>
        /// A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Adds a sink that writes log events as records in an Azure Table Storage table (default LogEventEntity) using the given storage account.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="keyGenerator">The key generator used to create the PartitionKey and the RowKey for each log entry</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureTableStorage(
            this LoggerSinkConfiguration loggerConfiguration,
            TableServiceClient storageAccount,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null,
            string storageTableName = null,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false,
            ICloudTableProvider cloudTableProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));
            return AzureTableStorage(
                loggerConfiguration,
                new JsonFormatter(formatProvider: formatProvider, closingDelimiter: ""),
                storageAccount,
                restrictedToMinimumLevel,
                storageTableName,
                writeInBatches,
                period,
                batchPostingLimit,
                keyGenerator,
                bypassTableCreationValidation,
                cloudTableProvider);
        }

        /// <summary>
        /// Adds a sink that writes log events as records in Azure Table Storage table (default name LogEventEntity) using the given
        /// storage account connection string.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="connectionString">The Cloud Storage Account connection string to use to insert the log entries to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="keyGenerator">The key generator used to create the PartitionKey and the RowKey for each log entry</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureTableStorage(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null,
            string storageTableName = null,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false,
            ICloudTableProvider cloudTableProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            return AzureTableStorage(
                loggerConfiguration,
                new JsonFormatter(formatProvider: formatProvider, closingDelimiter: ""),
                connectionString,
                restrictedToMinimumLevel,
                storageTableName,
                writeInBatches,
                period,
                batchPostingLimit,
                keyGenerator,
                bypassTableCreationValidation,
                cloudTableProvider);
        }

        /// <summary>
        /// Adds a sink that writes log events as records in Azure Table Storage table (default name LogEventEntity) using the given
        /// storage account name and Shared Access Signature (SAS) URL.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="sharedAccessSignature">The storage account/table SAS key.</param>
        /// <param name="accountName">The storage account name.</param>
        /// <param name="tableEndpoint">The (optional) table endpoint. Only needed for testing.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="keyGenerator">The key generator used to create the PartitionKey and the RowKey for each log entry</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureTableStorage(
            this LoggerSinkConfiguration loggerConfiguration,
            string sharedAccessSignature,
            string accountName,
            Uri tableEndpoint = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null,
            string storageTableName = null,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            IKeyGenerator keyGenerator = null,
            ICloudTableProvider cloudTableProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrWhiteSpace(accountName)) throw new ArgumentNullException(nameof(accountName));
            if (string.IsNullOrWhiteSpace(sharedAccessSignature))
                throw new ArgumentNullException(nameof(sharedAccessSignature));
            return AzureTableStorage(
                loggerConfiguration,
                new JsonFormatter(formatProvider: formatProvider, closingDelimiter: ""),
                sharedAccessSignature,
                accountName,
                tableEndpoint,
                restrictedToMinimumLevel,
                storageTableName,
                writeInBatches,
                period,
                batchPostingLimit,
                keyGenerator,
                cloudTableProvider);
        }

        /// <summary>
        /// Adds a sink that writes log events as records in an Azure Table Storage table (default LogEventEntity) using the given storage account.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Use a Serilog ITextFormatter such as CompactJsonFormatter to store object in data column of Azure table</param>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="keyGenerator">The key generator used to create the PartitionKey and the RowKey for each log entry</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureTableStorage(
            this LoggerSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            TableServiceClient storageAccount,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string storageTableName = null,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false,
            ICloudTableProvider cloudTableProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

            ILogEventSink sink;
            try
            {
                sink = writeInBatches ?
                    (ILogEventSink)new AzureBatchingTableStorageSink(storageAccount, formatter, batchPostingLimit ?? DefaultBatchPostingLimit, period ?? DefaultPeriod, storageTableName, keyGenerator, bypassTableCreationValidation, cloudTableProvider) :
                    new AzureTableStorageSink(storageAccount, formatter, storageTableName, keyGenerator, bypassTableCreationValidation, cloudTableProvider);
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine($"Error configuring AzureTableStorage: {ex}");
                sink = new LoggerConfiguration().CreateLogger();
            }

            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        /// <summary>
        /// Adds a sink that writes log events as records in Azure Table Storage table (default name LogEventEntity) using the given
        /// storage account connection string.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Use a Serilog ITextFormatter such as CompactJsonFormatter to store object in data column of Azure table</param>
        /// <param name="connectionString">The Cloud Storage Account connection string to use to insert the log entries to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="keyGenerator">The key generator used to create the PartitionKey and the RowKey for each log entry</param>
        /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureTableStorage(
            this LoggerSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            string connectionString,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string storageTableName = null,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            IKeyGenerator keyGenerator = null,
            bool bypassTableCreationValidation = false,
            ICloudTableProvider cloudTableProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            try
            {
                var storageAccount = new TableServiceClient(connectionString);
                return AzureTableStorage(loggerConfiguration, formatter, storageAccount, restrictedToMinimumLevel, storageTableName, writeInBatches, period, batchPostingLimit, keyGenerator, bypassTableCreationValidation, cloudTableProvider);
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine($"Error configuring AzureTableStorage: {ex}");

                ILogEventSink sink = new LoggerConfiguration().CreateLogger();
                return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
            }
        }

        /// <summary>
        /// Adds a sink that writes log events as records in Azure Table Storage table (default name LogEventEntity) using the given
        /// storage account name and Shared Access Signature (SAS) URL.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Use a Serilog ITextFormatter such as CompactJsonFormatter to store object in data column of Azure table</param>
        /// <param name="sharedAccessSignature">The storage account/table SAS key.</param>
        /// <param name="accountName">The storage account name.</param>
        /// <param name="tableEndpoint">The (optional) table endpoint. Only needed for testing.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="keyGenerator">The key generator used to create the PartitionKey and the RowKey for each log entry</param>
        /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureTableStorage(
            this LoggerSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            string sharedAccessSignature,
            string accountName,
            Uri tableEndpoint = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string storageTableName = null,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            IKeyGenerator keyGenerator = null,
            ICloudTableProvider cloudTableProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (string.IsNullOrWhiteSpace(accountName)) throw new ArgumentNullException(nameof(accountName));
            if (string.IsNullOrWhiteSpace(sharedAccessSignature)) throw new ArgumentNullException(nameof(sharedAccessSignature));

            try
            {
                var credentials = new AzureSasCredential(sharedAccessSignature);
                TableServiceClient storageAccount = null;
                if (tableEndpoint == null)
                {
                    tableEndpoint = new Uri($"https://{accountName}.table.core.windows.net/");
                }
                storageAccount = new TableServiceClient(tableEndpoint, credentials);

                // We set bypassTableCreationValidation to true explicitly here as the the SAS URL might not have enough permissions to query if the table exists.
                return AzureTableStorage(loggerConfiguration, formatter, storageAccount, restrictedToMinimumLevel, storageTableName, writeInBatches, period, batchPostingLimit, keyGenerator, true, cloudTableProvider);
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine($"Error configuring AzureTableStorage: {ex}");

                ILogEventSink sink = new LoggerConfiguration().CreateLogger();
                return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
            }
        }

    }
}
