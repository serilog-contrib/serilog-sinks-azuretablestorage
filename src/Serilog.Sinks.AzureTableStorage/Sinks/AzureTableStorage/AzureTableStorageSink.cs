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

namespace Serilog.Sinks.AzureTableStorage;

/// <summary>
/// Writes log events as records to an Azure Table Storage table.
/// </summary>
public class AzureTableStorageSink : IBatchedLogEventSink
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly AzureTableStorageSinkOptions _options;
    private readonly IDocumentFactory _documentFactory;
    private readonly IKeyGenerator _keyGenerator;
    private readonly ITableClientFactory _tableClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTableStorageSink" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="tableServiceClient">The table service client.</param>
    public AzureTableStorageSink(AzureTableStorageSinkOptions options, TableServiceClient tableServiceClient)
        : this(options, tableServiceClient, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTableStorageSink" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="tableServiceClient">The table service client.</param>
    /// <param name="documentFactory">The document factory.</param>
    /// <param name="keyGenerator">The key generator.</param>
    /// <param name="tableClientFactory">The table client factory.</param>
    /// <exception cref="System.ArgumentNullException">options</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="options" /> is null</exception>
    public AzureTableStorageSink(
        AzureTableStorageSinkOptions options,
        TableServiceClient tableServiceClient,
        IDocumentFactory documentFactory,
        IKeyGenerator keyGenerator,
        ITableClientFactory tableClientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));

        _keyGenerator = keyGenerator ?? new DefaultKeyGenerator();
        _documentFactory = documentFactory ?? new DefaultDocumentFactory();
        _tableClientFactory = tableClientFactory ?? new DefaultTableClientFactory();
    }

    /// <summary>
    /// Emit a batch of log events, running asynchronously.
    /// </summary>
    /// <param name="batch">The batch of events to emit.</param>
    /// <returns></returns>
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        // write documents in batches by partition key
        var documentGroups = batch
            .Select(logEvent => _documentFactory.Create(logEvent, _options, _keyGenerator))
            .GroupBy(p => p.PartitionKey);

        var tableClient = _tableClientFactory.CreateTableClient(_options, _tableServiceClient);

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
    public Task OnEmptyBatchAsync() => Task.CompletedTask;
}
