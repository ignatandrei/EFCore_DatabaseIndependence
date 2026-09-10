using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DainEF.Providers.PostgreSql;

public sealed class PostgreSqlEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "PostgreSQL";

    string IDatabasePlugin.Description => "PostgreSQL Entity Framework Core Provider";

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext
    {
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            NpgsqlConnectionStringBuilder sb = new(connectionString);
            sb.Database = databaseName;
            connectionString = sb.ConnectionString;
        }

        DbContextOptionsBuilder<T> dbContextOptionsBuilder = new();
        dbContextOptionsBuilder = dbContextOptionsBuilder.UseNpgsql(connectionString);
        return dbContextOptionsBuilder;
    }
}
