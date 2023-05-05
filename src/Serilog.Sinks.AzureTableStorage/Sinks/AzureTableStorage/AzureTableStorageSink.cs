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
using System.Linq;
using System.Threading.Tasks;

using Azure.Data.Tables;

using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Writes log events as records to an Azure Table Storage table.
/// </summary>
public class AzureTableStorageSink : ILogEventSink, IBatchedLogEventSink
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly AzureTableStorageSinkOptions _options;
    private readonly IDocumentFactory _documentFactory;
    private readonly Lazy<TableClient> _tableClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTableStorageSink" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="tableServiceClient">The table service client.</param>
    public AzureTableStorageSink(AzureTableStorageSinkOptions options, TableServiceClient tableServiceClient)
        : this(options, tableServiceClient, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTableStorageSink" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="tableServiceClient">The table service client.</param>
    /// <param name="documentFactory">The document factory.</param>
    /// <param name="keyGenerator">The key generator.</param>
    /// <exception cref="System.ArgumentNullException">options</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="options" /> is null</exception>
    public AzureTableStorageSink(AzureTableStorageSinkOptions options, TableServiceClient tableServiceClient, IDocumentFactory documentFactory, IKeyGenerator keyGenerator)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));

        keyGenerator ??= new DefaultKeyGenerator(options);

        _documentFactory = documentFactory ?? new DefaultDocumentFactory(options, keyGenerator);

        _tableClient = new Lazy<TableClient>(CreateTableClient);
    }

    /// <summary>
    /// Emit the provided log event to the sink.
    /// </summary>
    /// <param name="logEvent">The log event to write.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    public void Emit(LogEvent logEvent)
    {
        var document = _documentFactory.Create(logEvent);
        var tableClient = _tableClient.Value;

        tableClient.AddEntity(document);
    }

    /// <summary>
    /// Emit a batch of log events, running asynchronously.
    /// </summary>
    /// <param name="batch">The batch of events to emit.</param>
    /// <returns></returns>
    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        // write documents in batches by partition key
        var documentGroups = batch
            .Select(_documentFactory.Create)
            .GroupBy(p => p.PartitionKey);

        var tableClient = _tableClient.Value;

        foreach (var documentGroup in documentGroups)
        {
            // create table transactions
            var transactionActions = documentGroup
                .Select(tableEntity => new TableTransactionAction(TableTransactionActionType.Add, tableEntity))
                .ToList();

            // can only send 100 transactions at a time
            foreach (var transactionBatch in transactionActions.Chunk(100))
                await tableClient.SubmitTransactionAsync(transactionBatch);
        }
    }

    /// <summary>
    /// Allows sinks to perform periodic work without requiring additional threads
    /// or timers (thus avoiding additional flush/shut-down complexity).
    /// </summary>
    /// <returns></returns>
    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }

    private TableClient CreateTableClient()
    {
        var tableName = _options.StorageTableName ?? "LogEvent";
        var tableClient = _tableServiceClient.GetTableClient(tableName);

        try
        {
            tableClient.CreateIfNotExists();
        }
        catch (Exception ex)
        {
            Debugging.SelfLog.WriteLine($"Failed to create table: {ex}");
            if (_options.BypassTableCreationValidation)
                return tableClient;

            throw;
        }

        return tableClient;
    }
}

