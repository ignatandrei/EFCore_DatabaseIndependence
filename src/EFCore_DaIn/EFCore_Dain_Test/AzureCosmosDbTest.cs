using DainEF.Providers.AzureCosmosDB;
using EFCore_DaIn;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using SampleDatabase;
using Testcontainers.CosmosDb;

namespace EFCore_Dain_Test;

public abstract class AzureCosmosDbTest : DatabaseContainerTest
{
    private readonly AzureCosmosDbEfCorePlugin _plugin;
    private readonly CosmosDbContainer _container;

    protected AzureCosmosDbTest(CosmosDbContainer dockerContainer)
        : base(dockerContainer, new AzureCosmosDbEfCorePlugin(() => dockerContainer.HttpClient))
    {
        _container = dockerContainer;
        _plugin = new AzureCosmosDbEfCorePlugin(() => dockerContainer.HttpClient);
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

    [Fact]
    public override async Task ConnectionStateReturnsOpen()
    {
        Assert.Equal(0, await NrDatabases(_dockerContainer.GetConnectionString(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public override async Task GenerateDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = _dockerContainer.GetConnectionString();
        var countBefore = await NrDatabases(connectionString, cancellationToken);
        var options = _plugin.GenerateDbContextOptionsBuilder<EmpContext>(connectionString, "EmpContextDatabase").Options;
        using var context = new EmpContext(options);

        Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
        Assert.Equal(countBefore + 1, await NrDatabases(connectionString, cancellationToken));
    }

    [Fact]
    public async Task ConnectionCheckSucceeds()
    {
        IDatabasePlugin plugin = _plugin;
        Assert.True(await plugin.TestConnectionAsync(_dockerContainer.GetConnectionString(), TestContext.Current.CancellationToken));
    }

}

public sealed class AzureCosmosDbDefaultConfiguration : AzureCosmosDbTest
{
    public AzureCosmosDbDefaultConfiguration()
        : base(new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview").Build())
    {
    }
}
