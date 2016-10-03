using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Sinks.AzureTableStorage
{
    static class TaskExtensions
    {
        public static bool SyncContextSafeWait(this Task task, int timeout = Timeout.Infinite)
        {
            var prevContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                // Wait so that the timer thread stays busy and thus
                // we know we're working when flushing.
                return task.Wait(timeout);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }
        }

    }
}
