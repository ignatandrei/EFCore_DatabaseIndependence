using DainEF.Providers.Mongo;
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
        plugin = new MongoEfCorePlugin();
        Assert.Equal(version, plugin.Version);
    }
}
