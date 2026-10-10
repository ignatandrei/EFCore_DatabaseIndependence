using DainEF.Providers.InMemory;
using Microsoft.EntityFrameworkCore;
using SampleDatabase;

namespace EFCore_Dain_Test;

public sealed class InMemoryTest
{
    [Fact]
    public void GenerateDatabase()
    {
        var plugin = new InMemoryEfCorePlugin();
        var optionsBuilder = plugin.GenerateDbContextOptionsBuilder<EmpContext>("fallback", "test-database");

        using var context = new EmpContext(optionsBuilder.Options);
        context.Database.EnsureCreated();

        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
    }

    [Fact]
    public void UsesConnectionStringWhenDatabaseNameIsNotProvided()
    {
        var plugin = new InMemoryEfCorePlugin();
        var optionsBuilder = plugin.GenerateDbContextOptionsBuilder<EmpContext>("test-database");

        using var context = new EmpContext(optionsBuilder.Options);
        context.Database.EnsureCreated();

        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
    }
}
