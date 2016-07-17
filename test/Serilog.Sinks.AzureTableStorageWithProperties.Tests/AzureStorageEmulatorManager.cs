using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.AzureTableStorage.Tests
{
	// Start/stop azure storage emulator from code.
	// http://stackoverflow.com/questions/7547567/how-to-start-azure-storage-emulator-from-within-a-program
	public static class AzureStorageEmulatorManager
	{
		private const string _windowsAzureStorageEmulatorPath = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\WAStorageEmulator.exe";
        
        private const string _win7ProcessName = "WAStorageEmulator";
		private const string _win8ProcessName = "WASTOR~1";

        private const string _azureStorageEmulator4_4Path = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe";
        private const string _processName4_4 = "AzureStorageEmulator";

        private static readonly ProcessStartInfo startStorageEmulator = new ProcessStartInfo
        {
            FileName = _windowsAzureStorageEmulatorPath,
            Arguments = "start",
        };

        private static readonly ProcessStartInfo startStorageEmulator4_4 = new ProcessStartInfo
        {
            FileName = _azureStorageEmulator4_4Path,
            Arguments = "start",
        };

        private static readonly ProcessStartInfo stopStorageEmulator = new ProcessStartInfo
		{
			FileName = _windowsAzureStorageEmulatorPath,
			Arguments = "stop",
		};

        private static readonly ProcessStartInfo stopStorageEmulator4_4 = new ProcessStartInfo
        {
            FileName = _azureStorageEmulator4_4Path,
            Arguments = "stop",
        };

        private static Process GetProcess()
		{
			return Process.GetProcessesByName(_win7ProcessName).FirstOrDefault()
                ?? Process.GetProcessesByName(_win8ProcessName).FirstOrDefault()
                ?? Process.GetProcessesByName(_processName4_4).FirstOrDefault();
		}

		public static bool IsProcessStarted()
		{
			return GetProcess() != null;
		}

		public static void StartStorageEmulator()
		{
			if (!IsProcessStarted())
			{
                try
                {
                    using (Process process = Process.Start(startStorageEmulator4_4))
                    {
                        process.WaitForExit();
                    }
                }
                catch(System.ComponentModel.Win32Exception ex)
                {
                    if (ex.Message == "The system cannot find the file specified")
                    {
                        using (Process process = Process.Start(startStorageEmulator))
                        {
                            process.WaitForExit();
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
		}

		public static void StopStorageEmulator()
		{
            try
            {
                using (Process process = Process.Start(stopStorageEmulator4_4))
                {
                    process.WaitForExit();
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.Message == "The system cannot find the file specified")
                {
                    using (Process process = Process.Start(stopStorageEmulator))
                    {
                        process.WaitForExit();
                    }
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
