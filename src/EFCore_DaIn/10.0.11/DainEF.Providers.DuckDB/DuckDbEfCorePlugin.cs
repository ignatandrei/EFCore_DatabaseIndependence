using DuckDB.NET.Data;
using DuckDB.EFCoreProvider.Extensions;
using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.DuckDB;

public sealed class DuckDbEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "DuckDB";

    string IDatabasePlugin.Description => "DuckDB Entity Framework Core Provider (skuirrels)";

    bool IDatabasePlugin.NeedServer => false;

    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = new DuckDBConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
        catch (DuckDBException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }
    }

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext
        => UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null)
        where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var builder = new DuckDBConnectionStringBuilder { ConnectionString = connectionString };
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            builder.Remove("Data Source");
            builder.Remove("DataSource");
            builder.DataSource = databaseName;
        }

        return optionBuilder.UseDuckDB(builder.ConnectionString);
    }
}
