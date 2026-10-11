using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace DainEF.Providers.Oracle;

public sealed class OracleEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "Oracle";

    string IDatabasePlugin.Description => "Oracle Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => true;

    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (OracleException)
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

        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            var builder = new OracleConnectionStringBuilder(connectionString) { DataSource = databaseName };
            connectionString = builder.ConnectionString;
        }

        return optionBuilder.UseOracle(connectionString);
    }
}
