using DainEF.Providers.Mongo;
using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using Testcontainers.MongoDb;


namespace EFCore_Dain_Test;


public abstract class MongoTest : DatabaseContainerTest
{
    public MongoTest(DockerContainer dockerContainer) : base(dockerContainer, new MongoEfCorePlugin())
    {

    }
    public override async Task<int> NrDatabases(string connectionString, CancellationToken cancellationToken)
    {
        // Given
        var client = new MongoClient(connectionString);

        // When
        using var databases = await client.ListDatabasesAsync(cancellationToken);

        // Then
        var count = 0;
        while (await databases.MoveNextAsync(cancellationToken))
        {
            count += databases.Current.Count();
        }
        return count;
    }


    
}

public sealed class MongoDbDefaultConfiguration : MongoTest
{
    public MongoDbDefaultConfiguration()
        : base(
              new MongoDbBuilder("mongo:8.3")
              .WithUsername(UserName).WithPassword(Password)
              .Build())
    {
    }
}

public sealed class MongoDbNoAuthConfiguration : MongoTest
{
    public MongoDbNoAuthConfiguration()
        : base(new MongoDbBuilder("mongo:8.3").WithUsername(string.Empty).WithPassword(string.Empty).Build())
    {
    }
}
