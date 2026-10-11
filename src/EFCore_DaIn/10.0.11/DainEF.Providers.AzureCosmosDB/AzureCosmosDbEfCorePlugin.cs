using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.AzureCosmosDB;

public sealed class AzureCosmosDbEfCorePlugin : IEFCore_DatabasePlugin_10
{
    private readonly Func<HttpClient>? _httpClientFactory;

    public AzureCosmosDbEfCorePlugin()
    {
    }

    public AzureCosmosDbEfCorePlugin(Func<HttpClient> httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientFactory = httpClientFactory;
    }

    string IDatabasePlugin.ProviderName => "Azure Cosmos DB";

    string IDatabasePlugin.Description => "Azure Cosmos DB Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => true;

    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        cancellationToken.ThrowIfCancellationRequested();

        using var client = new CosmosClient(connectionString, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = _httpClientFactory
        });
        try
        {
            await client.ReadAccountAsync().WaitAsync(cancellationToken);
            return true;
        }
        catch (CosmosException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }
        catch (HttpRequestException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }
    }

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext
    {
        return UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);
    }

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null)
        where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        return optionBuilder.UseCosmos(connectionString, databaseName, options =>
        {
            options.ConnectionMode(ConnectionMode.Gateway);
            if (_httpClientFactory is not null)
            {
                options.HttpClientFactory(_httpClientFactory);
            }
        });
    }
}
