using DainEF.Providers.AzureCosmosDB;
using DainEF.Providers.InMemory;
using DainEF.Providers.Sqlite;
using DainEF.Providers.SqlServer;
using EFCore_DaIn;

namespace EFCore_Dain_Test_11;

public class TestInterface
{
    [Fact]
    public void TestVersion()
    {
        string version = "11.0.0";
        IDatabasePlugin plugin = new SqlServerEfCorePlugin();

        Assert.Equal(version, plugin.Version);
        plugin = new AzureCosmosDbEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new InMemoryEfCorePlugin();
        Assert.Equal(version, plugin.Version);
        plugin = new SqliteEfCorePlugin();
        Assert.Equal(version, plugin.Version);
    }
}
