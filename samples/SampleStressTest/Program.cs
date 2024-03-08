using Azure.Data.Tables;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace SampleStressTest;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var app = Host
                .CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddHostedService<StressTestService>()
                    .TryAddSingleton(sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        var connectionString = configuration.GetConnectionString("StorageAccount");
                        return new TableServiceClient(connectionString);
                    })
                )
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.AzureTableStorage(
                        storageAccount: services.GetRequiredService<TableServiceClient>(),
                        storageTableName: "SampleStressTest"
                    )
                )
                .Build();

            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
