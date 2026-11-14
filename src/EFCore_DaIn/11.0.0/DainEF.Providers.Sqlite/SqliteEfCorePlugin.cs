using EFCore_DaIn;
using EFCore_DaIn_11;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.Sqlite;

public sealed class SqliteEfCorePlugin : IEFCore_DatabasePlugin_11
{
    string IDatabasePlugin.ProviderName => "SQLite";

    string IDatabasePlugin.Description => "SQLite Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => false;

    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        cancellationToken.ThrowIfCancellationRequested();

        using var client = new SqliteConnection(connectionString);
        try
        {
            await client.OpenAsync(cancellationToken);
            return true;
        }
        catch (SqliteException)
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
            var builder = new SqliteConnectionStringBuilder(connectionString) { DataSource = databaseName };
            connectionString = builder.ConnectionString;
        }

        return optionBuilder.UseSqlite(connectionString);
    }
}
