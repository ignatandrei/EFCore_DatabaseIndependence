using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.SqlServer;

public sealed class SqlServerEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "SQL Server";

    string IDatabasePlugin.Description => "SQL Server Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => true;
    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SqlConnection(connectionString);
            // Test the connection by opening it
            await client.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString,string? databaseName =null)
        where T : DbContext
    {
        
        return UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);
    }

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null) where T : DbContext
    {
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            SqlConnectionStringBuilder sb = new(connectionString);
            sb.InitialCatalog = databaseName;
            connectionString = sb.ConnectionString;
        }
        optionBuilder = optionBuilder.UseSqlServer(connectionString);
        return optionBuilder;

    }
}
