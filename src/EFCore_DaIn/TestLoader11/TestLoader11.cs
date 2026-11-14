using DotNet.Testcontainers.Containers;
using EFCore_DaIn_11;
using EFCore_Dain_Settings;
using EFCore_DaInLoader;
using Microsoft.EntityFrameworkCore;
using SampleDatabase11;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Xunit.Sdk;

namespace TestLoader11;

[Collection("Loader")]
public class TestLoader11
{
    [Fact]
    public async Task TestLoad11()
    {

        //dotnet serve -p 51031
        //ProvidersIndexLoaders loaders = new ("http://localhost:51031");
        ProvidersIndexLoaders loaders = new("https://ignatandrei.github.io/");
        var results=await loaders.LoadFromUrlAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(results != null);
        var result = results[Version.Parse("11.0.0")];
        Assert.True(result.Providers?.Count > 0);
        var versions = result.VersionsEFCore();
        Assert.True(versions != null);
        Assert.True(versions?.Length > 0);
        foreach (var version in versions)
        {
            var runtimeOnCurrentOS = result.LoadProviders(version);
            Assert.NotNull(runtimeOnCurrentOS);
            foreach (var runtime in runtimeOnCurrentOS)
            {
                // Check if plugin is already downloaded to avoid re-downloading
                string pluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins",version.ToString(), runtime.Name!);
                string extractedPath = Path.Combine(pluginDirectory, runtime.Rid!);

                if (Directory.Exists(extractedPath))
                {
                    Console.WriteLine($"Plugin {runtime.Name} for {runtime.Rid} already downloaded. Skipping...");
                    continue;
                }
                await loaders.SaveProviderFromUrl(runtime,version, cancellationToken: TestContext.Current.CancellationToken);
            }
        }
    }
    protected static string UserName = "sa";
    protected static string Password = "Passw0rd!";

    DockerContainer StartDockerContainer(string name)
    {
        name = name.ToLowerInvariant();
        switch (true)
        {
            case var _ when name.Contains("postgres"):
                return new PostgreSqlBuilder("postgres:17-alpine")
                    .WithUsername(UserName)
                    .WithPassword(Password)
                    .Build();
            case var _ when name.Contains("mongo"):
                return new MongoDbBuilder("mongo:8.3")
                    .WithUsername(UserName)
                    .WithPassword(Password)
                    .Build();
            case var _ when name.Contains("sql server"):
                return new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithPassword(Password)
                    .Build();
            default:
                throw new NotSupportedException($"Unsupported database type: {name}");
        }
    }

    [Fact]
    public async Task TestLoad11_LoadOnePluginAfterTestLoad11()
    {
        await TestLoad11();


        var pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
        var discoveredAssemblies = EfCorePluginLoader_11.DiscoverPluginAssemblies(pluginsDirectory);
        Assert.True(discoveredAssemblies.Count > 0);
        foreach (var assembly in discoveredAssemblies)
        {
            Console.WriteLine($"Discovered assembly: {assembly}");
            Console.WriteLine("-----------------------------------");
        }
        foreach (var assembly in discoveredAssemblies)
        {
            var loaded = EfCorePluginLoader_11.LoadFromAssembly(assembly, isUnloadable: false);
            Assert.True(loaded.Count > 0);
            foreach (var plugin in loaded)
            {
                EmpContext ef;
                DockerContainer? cnt = null;
                if (plugin.ProviderName.Contains("sqlite", StringComparison.InvariantCultureIgnoreCase))
                {
                    string cn = Path.Combine(Path.GetTempPath(), $"efcore_dain_sqlite_{Guid.NewGuid():N}.db");
                    cn = $"Data Source={cn}";
                    ef = new EmpContext(plugin.GenerateDbContextOptionsBuilder<EmpContext>(cn, "EmpContext").Options);
                }
                else
                {
                    try
                    {
                        (cnt, ef) = await DockerPlugin(plugin);
                    }
                    catch(NotSupportedException)
                    {
                        Console.WriteLine($"Skipping plugin {plugin.ProviderName} as it is not supported on this OS.");
                        continue;
                    }
                }
                try
                {
                    await ef.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                    var nr = ef.Employee.Count();
                    Assert.Equal(0, nr);
                    var notTablesFound = ef.Problem_DBSets().ToArray();
                    Assert.Empty(notTablesFound);
                }
                finally
                {
                    if (cnt != null) await cnt.DisposeAsync();
                }
            }

        }
    }
    [Fact]
    public async Task TestLoad11_SaveAndRetrievePlugin()
    {
        await TestLoad11();

        DainEF_Data data =new DainEF_Data();
        data.PluginName = "InMemory";
        data.ConnectionString = "fake";
        data.DatabaseName= "fakeAgain";

        var b = await EfCorePluginLoader_11.SaveChosenPlugin(data);
        Assert.True(b,"must have saved");
        var (plugin,dataDain) = await EfCorePluginLoader_11.RetrieveLatestChosenPlugin();
        Assert.NotNull(plugin);
        Assert.NotNull(dataDain);
        Assert.Equal(plugin.ProviderName, data.PluginName);
        Assert.Equal(dataDain.PluginName, data.PluginName);
        Assert.Equal(dataDain.ConnectionString, data.ConnectionString);
        Assert.Equal(dataDain.DatabaseName, data.DatabaseName);


    }
    private async Task<(DockerContainer cnt, EmpContext ef)> DockerPlugin(IEFCore_DatabasePlugin_11 plugin)
    {
        var cnt = StartDockerContainer(plugin.ProviderName);
        await cnt.StartAsync(TestContext.Current.CancellationToken);
        var connectionString = cnt.GetConnectionString();
        Console.WriteLine($"Loaded plugin: {plugin.ProviderName} {plugin.Description}");
        Console.WriteLine("-----------------------------------");
        var opt = plugin.GenerateDbContextOptionsBuilder<EmpContext>(connectionString, "EmpContext");
        var ef = new EmpContext(opt.Options);
        return (cnt, ef);
    }
}
