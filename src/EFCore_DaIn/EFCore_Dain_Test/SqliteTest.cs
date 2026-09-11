using DainEF.Providers.Sqlite;
using Microsoft.Data.Sqlite;
using System.Data;
using WebAPI.Models;

namespace EFCore_Dain_Test;

public sealed class SqliteTest : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteEfCorePlugin _plugin;

    public SqliteTest()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"efcore_dain_sqlite_{Guid.NewGuid():N}.db");
        _plugin = new SqliteEfCorePlugin();
    }

    [Fact]
    public void GenerateDatabase()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath }.ConnectionString;
        var optionsBuilder = _plugin.GenerateDbContextOptionsBuilder<EmpContext>(connectionString, _databasePath);

        using var context = new EmpContext(optionsBuilder.Options);
        context.Database.EnsureCreated();

        Assert.True(File.Exists(_databasePath));
    }

    [Fact]
    public void ConnectionStateReturnsOpen()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath }.ConnectionString;
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        Assert.Equal(ConnectionState.Open, connection.State);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
            // SQLite may still hold the file lock briefly after the connection is disposed.
            // This is a best-effort cleanup for an ephemeral temp database.
        }
    }
}
