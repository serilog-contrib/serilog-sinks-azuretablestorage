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

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Sinks.AzureTableStorage
{
	/// <summary>
	/// Writes log events as records to an Azure Table Storage table storing properties as columns.
	/// </summary>
	public class AzureTableStorageWithPropertiesSink : ILogEventSink
	{
		private readonly IFormatProvider _formatProvider;
		private readonly CloudTable _table;
        private readonly string _additionalRowKeyPostfix;
        private Func<LogEvent, string, string> _rowKeyFunc = AzureTableStorageEntityFactory.GenerateValidRowKey;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="storageTableName">Table name that log entries will be written to. Note: Optional, setting this may impact performance</param>
		/// <param name="additionalRowKeyPostfix">Additional postfix string that will be appended to row keys</param>
        /// <param name="rowKeyFunc">Function to produce a row key</param>
        public AzureTableStorageWithPropertiesSink(CloudStorageAccount storageAccount, IFormatProvider formatProvider, string storageTableName = null, string additionalRowKeyPostfix = null, Func<LogEvent, string, string> rowKeyFunc = null)
        {
			var tableClient = storageAccount.CreateCloudTableClient();

			if (string.IsNullOrEmpty(storageTableName))
			{
				storageTableName = "LogEventEntity";
			}

			_table = tableClient.GetTableReference(storageTableName);
			_table.CreateIfNotExists();

			_formatProvider = formatProvider;

			if (additionalRowKeyPostfix != null)
			{
				_additionalRowKeyPostfix = AzureTableStorageEntityFactory.GetValidStringForTableKey(additionalRowKeyPostfix);
			}

            _rowKeyFunc = rowKeyFunc ?? _rowKeyFunc;
		}

		/// <summary>
		/// Emit the provided log event to the sink.
		/// </summary>
		/// <param name="logEvent">The log event to write.</param>
		public void Emit(LogEvent logEvent)
		{
			_table.Execute(TableOperation.Insert(AzureTableStorageEntityFactory.CreateEntityWithProperties(logEvent, _formatProvider, _additionalRowKeyPostfix, _rowKeyFunc)));
		}
	}
}
