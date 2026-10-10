using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.InMemory;

public sealed class InMemoryEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "InMemory";

    string IDatabasePlugin.Description => "InMemory Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => false;

    Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(true);
    }

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext
    {
        return UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);
    }

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null)
        where T : DbContext
    {
        var name = !string.IsNullOrWhiteSpace(databaseName) ? databaseName : connectionString;
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return optionBuilder.UseInMemoryDatabase(name);
    }
}
