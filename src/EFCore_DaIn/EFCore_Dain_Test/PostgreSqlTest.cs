using DainEF.Providers.PostgreSql;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;
using SampleDatabase;

namespace EFCore_Dain_Test;

public abstract class PostgreSqlTest : DatabaseContainerTest
{
    public PostgreSqlTest(DockerContainer dockerContainer) : base(dockerContainer, new PostgreSqlEfCorePlugin())
    {
    }

    public override int NrDatabases(string connectionString)
    {
        using var client = new NpgsqlConnection(connectionString);
        client.Open();

        using var cmd = client.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM pg_database WHERE datistemplate = false";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}

public class PostgreSqlDefaultConfiguration : PostgreSqlTest
{
    public PostgreSqlDefaultConfiguration()
        : base(new PostgreSqlBuilder("postgres:17-alpine")
              .WithUsername("postgres")
              .WithPassword("Passw0rd!")
              .Build())
    {
    }
}
