using DainEF.Providers.Oracle;
using DotNet.Testcontainers.Containers;
using EFCore_DaIn;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;

namespace EFCore_Dain_Test;

public abstract class OracleTest : DatabaseContainerTest
{
    private readonly OracleEfCorePlugin _plugin = new();

    protected OracleTest(DockerContainer dockerContainer)
        : base(dockerContainer, new OracleEfCorePlugin())
    {
    }

    public override async Task<int> NrDatabases(string connectionString, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM all_users";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    [Fact]
    public async Task ConnectionCheckReturnsFalseForInvalidCredentials()
    {
        var builder = new OracleConnectionStringBuilder(_dockerContainer.GetConnectionString())
        {
            Password = "IncorrectPassword123!",
            Pooling = false
        };
        IDatabasePlugin plugin = _plugin;

        Assert.False(await plugin.TestConnectionAsync(builder.ConnectionString, TestContext.Current.CancellationToken));
    }

    protected override string GetDatabaseName(string connectionString)
        => new OracleConnectionStringBuilder(connectionString).DataSource;

    protected override int ExpectedDatabaseCountIncrease => 0;
}

public sealed class OracleDefaultConfiguration : OracleTest
{
    public OracleDefaultConfiguration()
        : base(new OracleBuilder("gvenzl/oracle-free:23.26.3-slim-faststart")
            .WithUsername(UserName)
            .WithPassword(Password)
            .Build())
    {
    }
}
