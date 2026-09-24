using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.Sqlite;

public sealed class SqliteEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "SQLite";

    string IDatabasePlugin.Description => "SQLite Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => false;
    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SqliteConnection(connectionString);
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
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            SqliteConnectionStringBuilder sb = new(connectionString);
            sb.DataSource = databaseName;
            connectionString = sb.ConnectionString;
        }

        return UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);
    }

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null) where T : DbContext
    {
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            SqliteConnectionStringBuilder sb = new(connectionString);
            sb.DataSource = databaseName;
            connectionString = sb.ConnectionString;
        }
        optionBuilder = optionBuilder.UseSqlite(connectionString);
        return optionBuilder;
    }

}
