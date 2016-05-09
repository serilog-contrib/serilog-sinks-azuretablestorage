using Serilog.Events;

namespace Serilog.Sinks.AzureTableStorageKeyGenerators
{
    /// <summary>
    /// Interface used to generate row keys for events that are written in batches
    /// </summary>
    public interface IBatchKeyGenerator
    {
        /// <summary>
        /// Inform the generator that a new batch is about to be written
        /// </summary>
        void StartBatch();

        /// <summary>
        /// Generate the patition key based on the supplied <paramref name="logEvent"/>
        /// </summary>
        /// <param name="logEvent">the log event</param>
        /// <returns></returns>
        string GeneratePartitionKey(LogEvent logEvent);

        /// <summary>
        /// Generate a row key
        /// </summary>
        /// <param name="logEvent">the log event</param>
        /// <returns></returns>
        string GenerateRowKey(LogEvent logEvent);
    }
}