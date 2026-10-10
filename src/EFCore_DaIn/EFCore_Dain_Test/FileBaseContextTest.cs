using DainEF.Providers.FileBaseContext;
using EFCore_DaIn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SampleDatabase;
using System.Text.Json;

namespace EFCore_Dain_Test;

public sealed class FileBaseContextTest : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"efcore_dain_filebase_{Guid.NewGuid():N}");
    private readonly FileBaseContextEfCorePlugin _plugin = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("employees")]
    public void PersistsChangesAcrossContexts(string? databaseName)
    {
        var options = _plugin.GenerateDbContextOptionsBuilder<EmpContext>(_directory, databaseName)
            .EnableServiceProviderCaching(false).Options;
        using (var context = new EmpContext(options))
        {
            context.Department.Add(new Department { Id = 1, Name = "Engineering" });
            context.SaveChanges();
        }

        var location = !string.IsNullOrWhiteSpace(databaseName) ? Path.Combine(_directory, databaseName) : _directory;
        var jsonFile = Assert.Single(Directory.GetFiles(location, "*.json"));
        using (var json = JsonDocument.Parse(File.ReadAllText(jsonFile)))
        {
            Assert.Equal("Engineering", json.RootElement[0].GetProperty("Name").GetString());
        }

        using (var context = new EmpContext(_plugin.GenerateDbContextOptionsBuilder<EmpContext>(_directory, databaseName)
            .EnableServiceProviderCaching(false).Options))
        {
            var department = Assert.Single(context.Department);
            Assert.Equal("Engineering", department.Name);
            department.Name = "Research";
            context.SaveChanges();
        }

        using (var context = new EmpContext(_plugin.GenerateDbContextOptionsBuilder<EmpContext>(_directory, databaseName)
            .EnableServiceProviderCaching(false).Options))
        {
            var department = Assert.Single(context.Department);
            Assert.Equal("Research", department.Name);
            context.Department.Remove(department);
            context.SaveChanges();
        }

        using var emptyContext = new EmpContext(_plugin.GenerateDbContextOptionsBuilder<EmpContext>(_directory, databaseName)
            .EnableServiceProviderCaching(false).Options);
        Assert.Empty(emptyContext.Department);
    }

    [Fact]
    public void UsePluginPreservesExistingBuilder()
    {
        var builder = new DbContextOptionsBuilder<EmpContext>().EnableDetailedErrors();

        Assert.Same(builder, _plugin.UsePlugin(builder, _directory));
        Assert.True(Assert.IsType<CoreOptionsExtension>(builder.Options.FindExtension<CoreOptionsExtension>()).DetailedErrorsEnabled);
        using var context = new EmpContext(builder.Options);
        Assert.True(context.Database.CanConnect());
    }

    [Fact]
    public async Task ConnectionCheckCreatesDirectoryAndRemovesProbe()
    {
        IDatabasePlugin plugin = _plugin;

        Assert.Equal("FileBaseContext", plugin.ProviderName);
        Assert.False(plugin.NeedServer);
        Assert.True(await plugin.TestConnectionAsync(_directory, TestContext.Current.CancellationToken));
        Assert.True(Directory.Exists(_directory));
        Assert.Empty(Directory.GetFileSystemEntries(_directory));
    }

    [Fact]
    public async Task ConnectionCheckReturnsFalseForFilePath()
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, "not-a-directory");
        await File.WriteAllTextAsync(filePath, "existing content", TestContext.Current.CancellationToken);
        IDatabasePlugin plugin = _plugin;

        Assert.False(await plugin.TestConnectionAsync(filePath, TestContext.Current.CancellationToken));
        Assert.Equal("existing content", await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConnectionCheckHonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IDatabasePlugin plugin = _plugin;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => plugin.TestConnectionAsync(_directory, cancellation.Token));
        Assert.False(Directory.Exists(_directory));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RejectsMissingStorageDirectory(string? directory)
    {
        Assert.ThrowsAny<ArgumentException>(() => _plugin.GenerateDbContextOptionsBuilder<EmpContext>(directory!));
        IDatabasePlugin plugin = _plugin;
        await Assert.ThrowsAnyAsync<ArgumentException>(() => plugin.TestConnectionAsync(directory!, TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
