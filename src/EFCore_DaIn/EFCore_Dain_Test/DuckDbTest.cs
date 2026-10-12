using System.Data;
using DainEF.Providers.DuckDB;
using DuckDB.NET.Data;
using EFCore_DaIn;
using Microsoft.EntityFrameworkCore;
using SampleDatabase;

namespace EFCore_Dain_Test;

public sealed class DuckDbTest : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"efcore_dain_duckdb_{Guid.NewGuid():N}");
    private readonly DuckDbEfCorePlugin _plugin = new();
    private string DatabasePath => Path.Combine(_directory, "employees.duckdb");
    private string ConnectionString => new DuckDBConnectionStringBuilder { DataSource = DatabasePath }.ConnectionString;
    private EmpContext CreateContext() => new(_plugin.GenerateDbContextOptionsBuilder<EmpContext>(ConnectionString).Options);

    public DuckDbTest() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task GenerateDatabase()
    {
        using var context = CreateContext();
        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
        Assert.False(await context.Database.EnsureCreatedAsync(cancellationToken));
        Assert.True(File.Exists(DatabasePath));
        Assert.Empty(await context.Department.ToArrayAsync(cancellationToken));
        Assert.Empty(await context.Employee.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task ConnectionCheckSucceeds()
    {
        IDatabasePlugin plugin = _plugin;
        Assert.False(plugin.NeedServer);
        Assert.Equal("DuckDB", plugin.ProviderName);
        Assert.True(await plugin.TestConnectionAsync(ConnectionString, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConnectionStateReturnsOpen()
    {
        await using var connection = new DuckDBConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task InvalidConnectionStringsAreRejected(string? connectionString)
    {
        Assert.ThrowsAny<ArgumentException>(() => _plugin.GenerateDbContextOptionsBuilder<EmpContext>(connectionString!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => ((IDatabasePlugin)_plugin)
            .TestConnectionAsync(connectionString!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ((IDatabasePlugin)_plugin)
            .TestConnectionAsync(ConnectionString, cancellation.Token));
    }

    [Fact]
    public async Task MissingDirectoryReturnsFalse()
    {
        var connectionString = new DuckDBConnectionStringBuilder
        {
            DataSource = Path.Combine(_directory, "missing", "test.duckdb")
        }.ConnectionString;
        Assert.False(await ((IDatabasePlugin)_plugin)
            .TestConnectionAsync(connectionString, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Data Source=unused.duckdb")]
    [InlineData("DataSource=unused.duckdb")]
    public void DatabaseNameOverridesFileAndUsePluginPreservesBuilder(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<EmpContext>().EnableDetailedErrors();
        Assert.Same(builder, _plugin.UsePlugin(builder, connectionString, DatabasePath));
        using var context = new EmpContext(builder.Options);
        Assert.IsType<DuckDBConnection>(context.Database.GetDbConnection());
        Assert.Equal(DatabasePath, new DuckDBConnectionStringBuilder { ConnectionString = context.Database.GetConnectionString()! }.DataSource);
        using var generated = new EmpContext(_plugin.GenerateDbContextOptionsBuilder<EmpContext>(
            connectionString, DatabasePath).Options);
        Assert.Equal(context.Database.GetConnectionString(), generated.Database.GetConnectionString());
    }

    [Fact]
    public async Task CreatesTablesAndPersistsChangesAcrossContexts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        int departmentId;
        using (var context = CreateContext())
        {
            Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
            var department = new Department { Name = "Engineering" };
            context.Department.Add(department);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            Assert.True(department.Id > 0);
            departmentId = department.Id;
            var employee = new Employee { Name = "Andrei Ignat", Iddepartment = departmentId, IddepartmentNavigation = department };
            context.Employee.Add(employee);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            Assert.True(employee.Id > 0);
        }
        using (var context = CreateContext())
        {
            var employee = await context.Employee.Include(e => e.IddepartmentNavigation).SingleAsync(cancellationToken);
            Assert.Equal("Andrei Ignat", employee.Name);
            Assert.Equal(departmentId, employee.Iddepartment);
            Assert.Equal("Engineering", employee.IddepartmentNavigation!.Name);
            employee.Name = "Updated";
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }
        using (var context = CreateContext())
        {
            var employee = await context.Employee.SingleAsync(cancellationToken);
            Assert.Equal("Updated", employee.Name);
            context.Employee.Remove(employee);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            context.Department.Remove(await context.Department.SingleAsync(cancellationToken));
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }
        using var empty = CreateContext();
        Assert.Empty(await empty.Employee.ToArrayAsync(cancellationToken));
        Assert.Empty(await empty.Department.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task TransactionRollbackDoesNotPersistChanges()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            context.Department.Add(new Department { Name = "Rolled back" });
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            await transaction.RollbackAsync(cancellationToken);
        }
        using var verification = CreateContext();
        Assert.Empty(await verification.Department.ToArrayAsync(cancellationToken));
    }

    public void Dispose()
    {
        File.Delete(DatabasePath);
        File.Delete(DatabasePath + ".wal");
        Directory.Delete(_directory);
    }
}
