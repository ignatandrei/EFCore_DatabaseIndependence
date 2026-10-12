using DainEF.Providers.AzureCosmosDB;
using DainEF.Providers.FileBaseContext;
using DainEF.Providers.DuckDB;
using DainEF.Providers.InMemory;
using DainEF.Providers.Mongo;
using DainEF.Providers.Oracle;
using DainEF.Providers.PostgreSql;
using DainEF.Providers.Sqlite;
using DainEF.Providers.SqlServer;
using EFCore_DaIn;

namespace EFCore_Dain_Test;

public class TestInterface
{
    [Fact]
    public void TestVersion()
    {
        string version = "10.0.11";
        IDatabasePlugin plugin = new SqlServerEfCorePlugin();

        Assert.Equal(version, plugin.Version);
        plugin = new DuckDbEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new AzureCosmosDbEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new InMemoryEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new FileBaseContextEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new MongoEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new OracleEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new PostgreSqlEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new SqliteEfCorePlugin();
        Assert.Equal(version, plugin.Version);
    }
}
