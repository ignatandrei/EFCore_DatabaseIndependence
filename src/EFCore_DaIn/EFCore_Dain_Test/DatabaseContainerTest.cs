using DainEF.Providers.SqlServer;
using DotNet.Testcontainers.Containers;
using EFCore_DaIn_10;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SampleDatabase;

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
    public abstract Task<int> NrDatabases(string connectionString, CancellationToken cancellationToken);

    protected virtual string GetDatabaseName(string connectionString) => "EmpContextDatabase";

    protected virtual int ExpectedDatabaseCountIncrease => 1;

    [Fact]
    public async Task ConnectionCheckSucceeds()
    {
        Assert.True(await plugin.TestConnectionAsync(
            _dockerContainer.GetConnectionString(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreatesTablesAndPersistsChangesAcrossContexts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = _dockerContainer.GetConnectionString();
        var options = plugin.GenerateDbContextOptionsBuilder<EmpContext>(
            connectionString, GetDatabaseName(connectionString)).Options;

        using (var context = new EmpContext(options))
        {
            Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
            var department = new Department { Name = "Engineering" };
            if (context.Entry(department).Property(d => d.Id).Metadata.ValueGenerated == ValueGenerated.Never)
            {
                department.Id = 1;
            }
            context.Department.Add(department);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            Assert.True(department.Id > 0);
            var employee = new Employee
            {
                Name = "Andrei Ignat",
                Iddepartment = department.Id,
                IddepartmentNavigation = department
            };
            if (context.Entry(employee).Property(e => e.Id).Metadata.ValueGenerated == ValueGenerated.Never)
            {
                employee.Id = 1;
            }
            context.Employee.Add(employee);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }

        using (var context = new EmpContext(options))
        {
            var employee = await context.Employee.SingleAsync(cancellationToken);
            var department = await context.Department.SingleAsync(cancellationToken);
            Assert.Equal("Andrei Ignat", employee.Name);
            Assert.Equal("Engineering", department.Name);
            Assert.Equal(department.Id, employee.Iddepartment);
            employee.Name = "Andrei Ignat Updated";
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }

        using (var context = new EmpContext(options))
        {
            var employee = await context.Employee.SingleAsync(cancellationToken);
            Assert.Equal("Andrei Ignat Updated", employee.Name);
            context.Employee.Remove(employee);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            context.Department.Remove(await context.Department.SingleAsync(cancellationToken));
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }

        using var emptyContext = new EmpContext(options);
        Assert.Empty(await emptyContext.Employee.ToArrayAsync(cancellationToken));
        Assert.Empty(await emptyContext.Department.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task GenerateDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cn = _dockerContainer.GetConnectionString();
        var nrDatabasesBefore = await NrDatabases(cn, cancellationToken);
        var optBuilder = plugin.GenerateDbContextOptionsBuilder<EmpContext>(cn, GetDatabaseName(cn));
        var opt = optBuilder.Options;
        using EmpContext context = new EmpContext(opt);
        Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));

        var nrDatabasesAfter = await NrDatabases(cn, cancellationToken);
        Assert.Equal(nrDatabasesBefore + ExpectedDatabaseCountIncrease, nrDatabasesAfter);
        Assert.Empty(await context.Department.ToArrayAsync(cancellationToken));
        Assert.Empty(await context.Employee.ToArrayAsync(cancellationToken));

    }
    [Fact]
    public async Task ConnectionStateReturnsOpen()
    {
        var nrDatabasesBefore = await NrDatabases(_dockerContainer.GetConnectionString(), TestContext.Current.CancellationToken);
        Assert.True(nrDatabasesBefore >= 0);
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
