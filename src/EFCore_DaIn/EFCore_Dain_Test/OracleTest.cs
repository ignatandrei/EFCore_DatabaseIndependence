using DainEF.Providers.Oracle;
using DotNet.Testcontainers.Containers;
using EFCore_DaIn;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SampleDatabase;
using Testcontainers.Oracle;

namespace EFCore_Dain_Test;

public abstract class OracleTest : DatabaseContainerTest
{
    private readonly OracleEfCorePlugin _plugin = new();

    protected OracleTest(DockerContainer dockerContainer)
        : base(dockerContainer, new OracleEfCorePlugin())
    {
    }

    public override int NrDatabases(string connectionString)
    {
        using var connection = new OracleConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM all_users";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    [Fact]
    public override void GenerateDatabase()
    {
        var connectionString = _dockerContainer.GetConnectionString();
        var dataSource = new OracleConnectionStringBuilder(connectionString).DataSource;
        var options = _plugin.GenerateDbContextOptionsBuilder<EmpContext>(connectionString, dataSource).Options;
        using var context = new EmpContext(options);

        Assert.True(context.Database.EnsureCreated());

        using var connection = new OracleConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM user_tables WHERE table_name IN ('Department', 'Employee')";
        Assert.Equal(2, Convert.ToInt32(command.ExecuteScalar()));
    }

    [Fact]
    public async Task ConnectionCheckSucceeds()
    {
        IDatabasePlugin plugin = _plugin;

        Assert.True(await plugin.TestConnectionAsync(_dockerContainer.GetConnectionString(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConnectionCheckReturnsFalseForInvalidCredentials()
    {
        var builder = new OracleConnectionStringBuilder(_dockerContainer.GetConnectionString())
        {
            Password = "IncorrectPassword123!",
            Pooling = false
        };
        IDatabasePlugin plugin = _plugin;

        Assert.False(await plugin.TestConnectionAsync(builder.ConnectionString, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreatesTablesAndPersistsChangesAcrossContexts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = _dockerContainer.GetConnectionString();
        var dataSource = new OracleConnectionStringBuilder(connectionString).DataSource;
        var options = _plugin.GenerateDbContextOptionsBuilder<EmpContext>(connectionString, dataSource).Options;

        using (var context = new EmpContext(options))
        {
            Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
            var department = new Department { Name = "Engineering" };
            context.Department.Add(department);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            Assert.True(department.Id > 0);
            context.Employee.Add(new Employee
            {
                Name = "Alex",
                Iddepartment = department.Id,
                IddepartmentNavigation = department
            });
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }

        using (var context = new EmpContext(options))
        {
            var employee = await context.Employee.Include(e => e.IddepartmentNavigation).SingleAsync(cancellationToken);
            Assert.Equal("Alex", employee.Name);
            Assert.Equal("Engineering", employee.IddepartmentNavigation.Name);
            Assert.Equal(employee.IddepartmentNavigation.Id, employee.Iddepartment);
            employee.Name = "Alex Updated";
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }

        using (var context = new EmpContext(options))
        {
            var employee = await context.Employee.SingleAsync(cancellationToken);
            Assert.Equal("Alex Updated", employee.Name);
            context.Employee.Remove(employee);
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            context.Department.Remove(await context.Department.SingleAsync(cancellationToken));
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
        }

        using var emptyContext = new EmpContext(options);
        Assert.Empty(await emptyContext.Employee.ToListAsync(cancellationToken));
        Assert.Empty(await emptyContext.Department.ToListAsync(cancellationToken));
    }
}

public sealed class OracleDefaultConfiguration : OracleTest
{
    public OracleDefaultConfiguration()
        : base(new OracleBuilder("gvenzl/oracle-free:23.26.3-slim-faststart")
            .WithUsername(UserName)
            .WithPassword(Password)
            .Build())
    {
    }
}
