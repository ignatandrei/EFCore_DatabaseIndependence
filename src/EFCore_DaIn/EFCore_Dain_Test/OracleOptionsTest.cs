using DainEF.Providers.Oracle;
using EFCore_DaIn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using SampleDatabase;

namespace EFCore_Dain_Test;

public sealed class OracleOptionsTest
{
    private const string ConnectionString = "User Id=testuser;Password=testpassword;Data Source=localhost:1521/FREEPDB1;Pooling=false";
    private readonly OracleEfCorePlugin _plugin = new();

    [Fact]
    public void MetadataMatchesProviderContract()
    {
        IDatabasePlugin plugin = _plugin;

        Assert.Equal("Oracle", plugin.ProviderName);
        Assert.Equal("Oracle Entity Framework Core Provider", plugin.Description);
        Assert.Equal("10.0.11", plugin.Version);
        Assert.True(plugin.NeedServer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GeneratesOracleOptionsWithoutChangingConnectionString(string? databaseName)
    {
        using var context = new EmpContext(_plugin.GenerateDbContextOptionsBuilder<EmpContext>(ConnectionString, databaseName).Options);

        Assert.Equal("Oracle.EntityFrameworkCore", context.Database.ProviderName);
        Assert.Equal(ConnectionString, context.Database.GetConnectionString());
        Assert.IsType<OracleConnection>(context.Database.GetDbConnection());
    }

    [Theory]
    [InlineData("otherhost:1522/OTHERPDB")]
    [InlineData("MY_TNS_ALIAS")]
    [InlineData("(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)))")]
    public void DatabaseNameOverridesOnlyDataSource(string databaseName)
    {
        using var context = new EmpContext(_plugin.GenerateDbContextOptionsBuilder<EmpContext>(ConnectionString, databaseName).Options);
        var actual = new OracleConnectionStringBuilder(context.Database.GetConnectionString());
        var expected = new OracleConnectionStringBuilder(ConnectionString) { DataSource = databaseName };

        Assert.Equal(expected.ConnectionString, actual.ConnectionString);
    }

    [Fact]
    public void UsePluginPreservesExistingBuilderAndOptions()
    {
        var builder = new DbContextOptionsBuilder<EmpContext>().EnableDetailedErrors();

        Assert.Same(builder, _plugin.UsePlugin(builder, ConnectionString, "MY_TNS_ALIAS"));
        Assert.True(Assert.IsType<CoreOptionsExtension>(builder.Options.FindExtension<CoreOptionsExtension>()).DetailedErrorsEnabled);
        using var context = new EmpContext(builder.Options);
        Assert.Equal("MY_TNS_ALIAS", new OracleConnectionStringBuilder(context.Database.GetConnectionString()).DataSource);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RejectsMissingConnectionString(string? connectionString)
    {
        Assert.ThrowsAny<ArgumentException>(() => _plugin.GenerateDbContextOptionsBuilder<EmpContext>(connectionString!));
        IDatabasePlugin plugin = _plugin;
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => plugin.TestConnectionAsync(connectionString!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void RejectsNullOptionsBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => _plugin.UsePlugin<EmpContext>(null!, ConnectionString));
    }

    [Fact]
    public async Task ConnectionCheckHonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IDatabasePlugin plugin = _plugin;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => plugin.TestConnectionAsync(ConnectionString, cancellation.Token));
    }

    [Fact]
    public async Task ConnectionCheckRejectsMalformedConnectionString()
    {
        IDatabasePlugin plugin = _plugin;

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => plugin.TestConnectionAsync("UnsupportedKey=value", TestContext.Current.CancellationToken));
    }
}
