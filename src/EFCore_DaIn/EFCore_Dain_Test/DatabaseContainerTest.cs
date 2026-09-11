using DainEF.Providers.SqlServer;
using DotNet.Testcontainers.Containers;
using EFCore_DaIn_10;
using WebAPI.Models;

namespace EFCore_Dain_Test;

public abstract partial class DatabaseContainerTest : IAsyncLifetime
{
    private protected readonly DockerContainer _dockerContainer;
    private readonly IEFCore_DatabasePlugin plugin;
    protected static string UserName = "sa";
    protected static string Password = "Passw0rd!";

    protected DatabaseContainerTest(DockerContainer dockerContainer, IEFCore_DatabasePlugin plugin)
    {
        _dockerContainer = dockerContainer;
        this.plugin = plugin;
    }
    public abstract int NrDatabases(string connectionString);
    [Fact]
    public void GenerateDatabase()
    {
        var nrDatabasesBefore = NrDatabases(_dockerContainer.GetConnectionString());

        var cn = _dockerContainer.GetConnectionString();
        var optBuilder = plugin.GenerateDbContextOptionsBuilder<EmpContext>(cn, "EmpContextDatabase");
        var opt = optBuilder.Options;
        using EmpContext context = new EmpContext(opt);
        context.Database.EnsureCreated();

        var nrDatabasesAfter = NrDatabases(_dockerContainer.GetConnectionString());
        Assert.Equal(nrDatabasesBefore + 1, nrDatabasesAfter);

    }
    [Fact]
    public void ConnectionStateReturnsOpen()
    {
        Console.WriteLine("!!!" + _dockerContainer.GetConnectionString());
        var nrDatabasesBefore = NrDatabases(_dockerContainer.GetConnectionString());
        Assert.True(nrDatabasesBefore > 0);
    }

    public async ValueTask InitializeAsync()
    {
        await _dockerContainer.StartAsync()
            .ConfigureAwait(false);
    }   

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore()
            .ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    
    protected virtual async Task DisposeAsyncCore()
    {
        await _dockerContainer.DisposeAsync();
    }
        
    
}
