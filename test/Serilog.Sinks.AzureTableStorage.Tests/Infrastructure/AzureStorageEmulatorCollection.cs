using Xunit;

namespace Serilog.Sinks.AzureTableStorage.Tests.Infrastructure
{
    [CollectionDefinition("AzureStorageIntegrationTests")]
    public class AzureStorageEmulatorCollection : ICollectionFixture<AzureStorageEmulatorFixture>
    {
    }
}
