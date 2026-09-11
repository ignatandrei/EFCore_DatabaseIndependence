using EFCore_DaIn;
using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.Mongo;

public class MongoEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "MongoDB";

    string IDatabasePlugin.Description => "MongoDB Entity Framework Core Provider";

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString,string? databaseName =null)
        where T : DbContext
    {
        return UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);
    }

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null) where T : DbContext
    {
        if(string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = "PleaseProvideADatabaseName";
        }
        optionBuilder = optionBuilder.UseMongoDB(connectionString, databaseName);
        return optionBuilder;
    }
}
