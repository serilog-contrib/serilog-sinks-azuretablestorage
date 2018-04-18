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

namespace Serilog.Sinks.AzureTableStorage.AzureTableProvider
{
    public class DefaultCloudTableProvider : ICloudTableProvider
    {
        private readonly int _waitTimeoutMilliseconds = Timeout.Infinite;
        private readonly CloudTableClient _cloudTableClient;
        private readonly string _storageTableName;
        private readonly bool _bypassTableCreationValidation;
        private CloudTable _cloudTable;

        public DefaultCloudTableProvider(CloudStorageAccount storageAccount,
                                         string storageTableName,
                                         bool bypassTableCreationValidation)
        {
            _cloudTableClient = storageAccount.CreateCloudTableClient();
            _storageTableName = storageTableName;
            _bypassTableCreationValidation = bypassTableCreationValidation;
        }

        public CloudTable GetCloudTable()
        {
            if (_cloudTable == null)
            {
                _cloudTable = _cloudTableClient.GetTableReference(_storageTableName);

                // In some cases (e.g.: SAS URI), we might not have enough permissions to create the table if
                // it does not already exists. So, if we are in that case, we ignore the error as per bypassTableCreationValidation.
                try
                {
                    _cloudTable.CreateIfNotExistsAsync().SyncContextSafeWait(_waitTimeoutMilliseconds);
                }
                catch (Exception ex)
                {
                    Debugging.SelfLog.WriteLine($"Failed to create table: {ex}");
                    if (!_bypassTableCreationValidation)
                    {
                        throw;
                    }
                }
            }
            return _cloudTable;
        }
    }
}
