using DotNet.Testcontainers.Containers;
using EFCore_DaIn_10;
using EFCoreLoader;
using SampleDatabase;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
namespace TestLoader;

public class TestLoader
{
    [Fact]
    public async Task TestLoad10()
    {

        //dotnet serve -p 51031
        //ProvidersIndexLoaders loaders = new ("http://localhost:51031");
        ProvidersIndexLoaders loaders = new("https://ignatandrei.github.io/");
        var result=await loaders.LoadFromUrlAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result != null);
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
                string pluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins", runtime.Name!);
                string extractedPath = Path.Combine(pluginDirectory, runtime.Rid!);

                if (Directory.Exists(extractedPath))
                {
                    Console.WriteLine($"Plugin {runtime.Name} for {runtime.Rid} already downloaded. Skipping...");
                    continue;
                }
                await loaders.SaveProviderFromUrl(runtime, cancellationToken: TestContext.Current.CancellationToken);
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
    public async Task TestLoad10_LoadOnePluginAfterTestLoad10()
    {
        await TestLoad10();


        var pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
        var discoveredAssemblies = EfCorePluginLoader_10.DiscoverPluginAssemblies(pluginsDirectory);
        Assert.True(discoveredAssemblies.Count > 0);
        foreach (var assembly in discoveredAssemblies)
        {
            Console.WriteLine($"Discovered assembly: {assembly}");
            Console.WriteLine("-----------------------------------");
        }
        foreach (var assembly in discoveredAssemblies)
        {
            var loaded = EfCorePluginLoader_10.LoadFromAssembly(assembly, isUnloadable: false);
            Assert.True(loaded.Plugins.Count > 0);
            foreach (var plugin in loaded.Plugins)
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
                    (cnt, ef) = await DockerPlugin(plugin);
                }
                try
                {
                    await ef.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                    var nr = ef.Employee.Count();
                    Assert.Equal(0, nr);
                }
                finally
                {
                    if (cnt != null) await cnt.DisposeAsync();
                }
            }

        }
    }

    private async Task<(DockerContainer cnt, EmpContext ef)> DockerPlugin(IEFCore_DatabasePlugin_10 plugin)
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


