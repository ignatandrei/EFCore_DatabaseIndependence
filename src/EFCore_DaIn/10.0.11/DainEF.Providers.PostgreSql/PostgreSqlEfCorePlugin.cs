using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DainEF.Providers.PostgreSql;

public sealed class PostgreSqlEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "PostgreSQL";

    string IDatabasePlugin.Description => "PostgreSQL Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => true;
    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new NpgsqlConnection(connectionString);
            // Test the connection by opening it
            await client.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext
    {
        return UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);
    }

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null) where T : DbContext
    {
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            NpgsqlConnectionStringBuilder sb = new(connectionString);
            sb.Database = databaseName;
            connectionString = sb.ConnectionString;
        }
        optionBuilder = optionBuilder.UseNpgsql(connectionString);
        return optionBuilder;
    }
}
