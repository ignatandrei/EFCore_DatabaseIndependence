using EFCore_DaIn;
using Microsoft.EntityFrameworkCore;

namespace EFCore_DaIn_11;

public interface IEFCore_DatabasePlugin : IDatabasePlugin
{
    /// <summary>Creates options for a context using this provider.</summary>
    DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext;

    /// <summary>Configures an existing options builder using this provider.</summary>
    DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null)
        where T : DbContext;
}

public interface IEFCore_DatabasePlugin_11 : IEFCore_DatabasePlugin
{
    string IDatabasePlugin.Version => "11.0.0";
}
