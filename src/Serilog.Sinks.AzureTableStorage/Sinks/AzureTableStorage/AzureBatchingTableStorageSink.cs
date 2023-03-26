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
using System.Threading.Tasks;

using Azure.Data.Tables;

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.AzureTableStorage.AzureTableProvider;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Writes log events as records to an Azure Table Storage table.
/// </summary>
public class AzureBatchingTableStorageSink : PeriodicBatchingSink
{
    readonly ITextFormatter _textFormatter;
    readonly IKeyGenerator _keyGenerator;
    readonly TableServiceClient _storageAccount;
    readonly string _storageTableName;
    readonly bool _bypassTableCreationValidation;
    readonly ICloudTableProvider _cloudTableProvider;

    /// <summary>
    /// Construct a sink that saves logs to the specified storage account.
    /// </summary>
    /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
    /// <param name="textFormatter"></param>
    /// <param name="batchSizeLimit"></param>
    /// <param name="period"></param>
    /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
    /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
    public AzureBatchingTableStorageSink(
        TableServiceClient storageAccount,
        ITextFormatter textFormatter,
        int batchSizeLimit,
        TimeSpan period,
        string storageTableName = null,
        ICloudTableProvider cloudTableProvider = null)
        : this(storageAccount, textFormatter, batchSizeLimit, period, storageTableName, new DefaultKeyGenerator(), cloudTableProvider: cloudTableProvider)
    {
    }

    /// <summary>
    /// Construct a sink that saves logs to the specified storage account.
    /// </summary>
    /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
    /// <param name="textFormatter"></param>
    /// <param name="batchSizeLimit"></param>
    /// <param name="period"></param>
    /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
    /// <param name="keyGenerator">generator used for partition keys and row keys</param>
    /// <param name="bypassTableCreationValidation">Bypass the exception in case the table creation fails.</param>
    /// <param name="cloudTableProvider">Cloud table provider to get current log table.</param>
    public AzureBatchingTableStorageSink(
        TableServiceClient storageAccount,
        ITextFormatter textFormatter,
        int batchSizeLimit,
        TimeSpan period,
        string storageTableName = null,
        IKeyGenerator keyGenerator = null,
        bool bypassTableCreationValidation = false,
        ICloudTableProvider cloudTableProvider = null)
#pragma warning disable CS0618 // Type or member is obsolete
        : base(batchSizeLimit, period)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        if (batchSizeLimit < 1 || batchSizeLimit > 100)
            throw new ArgumentException("batchSizeLimit must be between 1 and 100 for Azure Table Storage");

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
    /// Emit a batch of log events, running asynchronously.
    /// </summary>
    /// <param name="events">The events to emit.</param>
    /// <remarks>
    /// Override either <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" /> or <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />,
    /// not both.
    /// </remarks>
    protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
    {
        var table = _cloudTableProvider.GetCloudTable(_storageAccount, _storageTableName, _bypassTableCreationValidation);
        var transactionActions = new List<TableTransactionAction>();

        string lastPartitionKey = null;

        foreach (var logEvent in events)
        {
            var partitionKey = _keyGenerator.GeneratePartitionKey(logEvent);

            if (partitionKey != lastPartitionKey)
            {
                lastPartitionKey = partitionKey;
                if (transactionActions.Count > 0)
                {
                    await table.SubmitTransactionAsync(transactionActions).ConfigureAwait(false);
                    transactionActions = new List<TableTransactionAction>();
                }
            }
            var logEventEntity = new LogEventEntity(
                logEvent,
                _textFormatter,
                partitionKey,
                _keyGenerator.GenerateRowKey(logEvent)
                );
            var transactionAction =
                new TableTransactionAction(TableTransactionActionType.UpdateMerge, logEventEntity);
            transactionActions.Add(transactionAction);
        }
        if (transactionActions.Count > 0)
        {
            await table.SubmitTransactionAsync(transactionActions).ConfigureAwait(false);
        }
    }
}
