using EFCore_DaIn;
using Microsoft.EntityFrameworkCore;

namespace EFCore_DaIn_10;

public interface IEFCore_DatabasePlugin : IDatabasePlugin
{
    
    DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext;

    DbContextOptionsBuilder<T>  UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder,string connectionString, string? databaseName = null)
        where T : DbContext;    

}
public interface IEFCore_DatabasePlugin_10 : IEFCore_DatabasePlugin
{
    string IDatabasePlugin.Version => "10.0.11";
}
