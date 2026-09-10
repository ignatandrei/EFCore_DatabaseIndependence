using DainEF.Providers.Mongo;
using DainEF.Providers.SqlServer;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using Testcontainers.MsSql;
using WebAPI.Models;

namespace EFCore_Dain_Test;

public abstract class SqlServerTest : DatabaseContainerTest
{
    public SqlServerTest(DockerContainer dockerContainer) : base(dockerContainer, new SqlServerEfCorePlugin())
    {

    }
    public override int  NrDatabases(string sqlConnection) {

        Console.WriteLine("!!!" + sqlConnection);

        using var client = new SqlConnection(sqlConnection);
        client.Open();

        // When
        var nrDatabasesBefore = 0;
        using var cmd = client.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM sys.databases";
        nrDatabasesBefore = (int)cmd.ExecuteScalar();

        return nrDatabasesBefore;
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
