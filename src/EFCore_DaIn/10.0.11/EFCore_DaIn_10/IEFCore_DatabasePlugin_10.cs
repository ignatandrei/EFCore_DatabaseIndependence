using EFCore_DaIn;
using EFCore_Dain_Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection.PortableExecutable;

namespace EFCore_DaIn_10;

public interface IEFCore_DatabasePlugin : IDatabasePlugin
{
    /// <summary>
    /// to be used 
    /// var optBuilder = plugin.GenerateDbContextOptionsBuilder<EmpContext>(cn, "EmpContextDatabase");
    /// var opt = optBuilder.Options;
    /// EmpContext context = new EmpContext(opt);
    ///
    /// </summary>
    /// <typeparam name="T"> the context from EF</typeparam>
    /// <param name="connectionString">connection string</param>
    /// <param name="databaseName">database name, optional</param>
    /// <returns></returns>
    DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext;

    /// <summary>
    /// to be used
    /// services.AddDbContextFactory<ApplicationDbContext>(
    ///options => plugin.UsePlugin(options, connectionString));
    /// </summary>
    /// <typeparam name="T"> the context from EF</typeparam>
    /// <param name="optionBuilder">the DbContextOptionsBuilder</param>
    /// <param name="connectionString">connection string</param>
    /// <param name="databaseName">database name, optional</param>
    /// <returns></returns>
    DbContextOptionsBuilder<T>  UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder,string connectionString, string? databaseName = null)
        where T : DbContext;
}
public interface IEFCore_DatabasePlugin_10 : IEFCore_DatabasePlugin
{
    string IDatabasePlugin.Version => "10.0.11";
}
