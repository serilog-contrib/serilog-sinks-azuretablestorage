using System;
using System.Threading.Tasks;

namespace Serilog.Sinks.AzureTableStorage.Tests.Infrastructure
{
    /// <summary>
    /// Adapted from https://github.com/kendaleiv/azure-storage-integration-tests
    /// </summary>
    public class AzureStorageEmulatorFixture : IDisposable
    {
        private readonly bool _wasStorageEmulatorUp;

        public AzureStorageEmulatorFixture()
        {
            if (!AzureStorageEmulatorManager.IsProcessStarted())
            {
                AzureStorageEmulatorManager.StartStorageEmulator();

                Console.WriteLine("delaying 500ms in the hopes that the storage emulator is started...");
                Task.Delay(500).Wait(500);

                _wasStorageEmulatorUp = false;
            }
            else
            {
                _wasStorageEmulatorUp = true;
            }
        }

        public void Dispose()
        {
            if (!_wasStorageEmulatorUp)
            {
                AzureStorageEmulatorManager.StopStorageEmulator();
            }
            else
            {
                // Leave as it was before testing...
            }
        }
    }
}
