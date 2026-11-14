using DainEF.Providers.SqlServer;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using SampleDatabase;

namespace EFCore_Dain_Test_11;

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
