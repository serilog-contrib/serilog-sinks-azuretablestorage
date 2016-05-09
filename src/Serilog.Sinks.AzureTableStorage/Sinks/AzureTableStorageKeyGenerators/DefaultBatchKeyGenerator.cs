namespace Serilog.Sinks.AzureTableStorageKeyGenerators
{
    class DefaultBatchKeyGenerator : DefaultKeyGenerator, IBatchKeyGenerator
    { 
        public void StartBatch()
        {
            RowId = 0L;
        }
    }
}