using EFCore_DaIn;
using EFCore_DaIn_11;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.SqlServer;

public sealed class SqlServerEfCorePlugin : IEFCore_DatabasePlugin_11
{
    string IDatabasePlugin.ProviderName => "SQL Server";

    string IDatabasePlugin.Description => "SQL Server Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => true;

    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        cancellationToken.ThrowIfCancellationRequested();

        using var client = new SqlConnection(connectionString);
        try
        {
            await client.OpenAsync(cancellationToken);
            return true;
        }
        catch (SqlException)
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
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName };
            connectionString = builder.ConnectionString;
        }

        return optionBuilder.UseSqlServer(connectionString);
    }
}
