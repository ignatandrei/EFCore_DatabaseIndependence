using DainEF.Providers.Mongo;
using DainEF.Providers.SqlServer;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using Testcontainers.MsSql;
using SampleDatabase;

namespace EFCore_Dain_Test;

public abstract class SqlServerTest : DatabaseContainerTest
{
    public SqlServerTest(DockerContainer dockerContainer) : base(dockerContainer, new SqlServerEfCorePlugin())
    {

    }
    public override async Task<int> NrDatabases(string sqlConnection, CancellationToken cancellationToken) {

        Console.WriteLine("!!!" + sqlConnection);

        using var client = new SqlConnection(sqlConnection);
        await client.OpenAsync(cancellationToken);

        using var cmd = client.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM sys.databases";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
    }
    

}
public class SqlServerDefaultConfiguration : SqlServerTest
{
    public SqlServerDefaultConfiguration()
        : base(
              new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
              .WithPassword(Password)
              .Build())
    {
    }
}
