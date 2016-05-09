using System.Threading;
using Serilog.Events;

namespace Serilog.Sinks.AzureTableStorageKeyGenerators
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
            var timeRoundedToMinute = utcEventTime.AddMilliseconds(-utcEventTime.Millisecond);
            return string.Format("0{0}", timeRoundedToMinute.Ticks);
        }

        public string GenerateRowKey(LogEvent logEvent)
        {
            return string.Format("{0}|{1}|{2}",
                logEvent.Level,
                logEvent.MessageTemplate.Text,
                Interlocked.Increment(ref RowId)
                );
        }
    }
}