using System.Threading;
using Serilog.Events;

namespace Serilog.Sinks.AzureTableStorage.KeyGenerator
{
    public class DefaultKeyGenerator : IKeyGenerator
    {
        protected long RowId;

        public DefaultKeyGenerator()
        {
            RowId = 0L;
        }

        /// <summary>
        /// Automatically generates the PartitionKey based on the logEvent timestamp
        /// </summary>
        /// <param name="logEvent">the log event</param>
        /// <returns>The Generated PartitionKey</returns>
        public virtual string GeneratePartitionKey(LogEvent logEvent)
        {
            var utcEventTime = logEvent.Timestamp.UtcDateTime;
            var timeWithoutMilliseconds = utcEventTime.AddMilliseconds(-utcEventTime.Millisecond);
            return $"0{timeWithoutMilliseconds.Ticks}";
        }

        /// <summary>
        /// Automatically generates the RowKey using the following template: {Level|MessageTemplate|IncrementedRowId}
        /// </summary>
        /// <param name="logEvent">the log event</param>
        /// <param name="suffix">Suffix to add to RowKey</param>
        /// <returns>The generated RowKey</returns>
        public virtual string GenerateRowKey(LogEvent logEvent, string suffix = null)
        {
            return $"{logEvent.Level}|{logEvent.MessageTemplate}|{Interlocked.Increment(ref RowId)}";
        }
    }
}
