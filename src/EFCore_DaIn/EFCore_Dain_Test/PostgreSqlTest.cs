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

    public override async Task<int> NrDatabases(string connectionString, CancellationToken cancellationToken)
    {
        using var client = new NpgsqlConnection(connectionString);
        await client.OpenAsync(cancellationToken);

        using var cmd = client.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM pg_database WHERE datistemplate = false";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
    }
}

public class PostgreSqlDefaultConfiguration : PostgreSqlTest
{
    public PostgreSqlDefaultConfiguration()
        : base(new PostgreSqlBuilder("postgres:17-alpine")
              .WithUsername(UserName)
              .WithPassword(Password)
              .Build())
    {
    }
}
