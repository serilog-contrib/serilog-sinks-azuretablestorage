using System.Threading;
using Serilog.Events;

namespace Serilog.Sinks.AzureTableStorage.KeyGenerator
{
    class DefaultKeyGenerator : IKeyGenerator
    {
        protected long RowId;

        public DefaultKeyGenerator()
        {
            RowId = 0L;
        }

        public string GeneratePartitionKey(LogEvent logEvent)
        {
            var utcEventTime = logEvent.Timestamp.UtcDateTime;
            var timeWithoutMilliseconds = utcEventTime.AddMilliseconds(-utcEventTime.Millisecond);
            return $"0{timeWithoutMilliseconds.Ticks}";
        }

        public string GenerateRowKey(LogEvent logEvent)
        {
            return $"{logEvent.Level}|{logEvent.MessageTemplate}|{Interlocked.Increment(ref RowId)}";
        }
    }
}
