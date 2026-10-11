using DainEF.Providers.AzureCosmosDB;
using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;

namespace EFCore_Dain_Test;

public abstract class AzureCosmosDbTest : DatabaseContainerTest
{
    private readonly CosmosDbContainer _container;

    protected AzureCosmosDbTest(CosmosDbContainer dockerContainer)
        : base(dockerContainer, new AzureCosmosDbEfCorePlugin(() => dockerContainer.HttpClient))
    {
        _container = dockerContainer;
    }

    public override async Task<int> NrDatabases(string connectionString, CancellationToken cancellationToken)
    {
        using var client = new CosmosClient(connectionString, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => _container.HttpClient
        });
        using var iterator = client.GetDatabaseQueryIterator<DatabaseProperties>();
        var count = 0;
        while (iterator.HasMoreResults)
        {
            count += (await iterator.ReadNextAsync(cancellationToken)).Count;
        }
        return count;
    }

}

public sealed class AzureCosmosDbDefaultConfiguration : AzureCosmosDbTest
{
    public AzureCosmosDbDefaultConfiguration()
        : base(new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview").Build())
    {
    }
}
