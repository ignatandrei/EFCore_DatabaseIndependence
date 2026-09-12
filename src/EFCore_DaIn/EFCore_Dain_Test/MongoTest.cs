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
    public override int NrDatabases(string connectionString)
    {
        // Given
        var client = new MongoClient(_dockerContainer.GetConnectionString());

        // When
        using var databases = client.ListDatabases(CancellationToken.None);

        // Then
        return databases.ToEnumerable(CancellationToken.None).Count();
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

