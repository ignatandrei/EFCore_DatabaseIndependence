using EFCore_DaIn;
using EFCore_DaIn_10;
using kDg.FileBaseContext.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DainEF.Providers.FileBaseContext;

public sealed class FileBaseContextEfCorePlugin : IEFCore_DatabasePlugin_10
{
    string IDatabasePlugin.ProviderName => "FileBaseContext";

    string IDatabasePlugin.Description => "FileBaseContext Entity Framework Core Provider";

    bool IDatabasePlugin.NeedServer => false;

    async Task<bool> IDatabasePlugin.TestConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Directory.CreateDirectory(connectionString);
            var probePath = Path.Combine(connectionString, $".dain-connection-{Guid.NewGuid():N}.tmp");
            await using var stream = new FileStream(probePath, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            await stream.WriteAsync(new byte[] { 1 }, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Position = 0;
            var buffer = new byte[1];
            return await stream.ReadAsync(buffer, cancellationToken) == 1 && buffer[0] == 1;
        }
        catch
        {
            return false;
        }

    }

    public DbContextOptionsBuilder<T> GenerateDbContextOptionsBuilder<T>(string connectionString, string? databaseName = null)
        where T : DbContext
    {
        return UsePlugin(new DbContextOptionsBuilder<T>(), connectionString, databaseName);
    }

    public DbContextOptionsBuilder<T> UsePlugin<T>(DbContextOptionsBuilder<T> optionBuilder, string connectionString, string? databaseName = null)
        where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var location = !string.IsNullOrWhiteSpace(databaseName)
            ? Path.Combine(connectionString, databaseName)
            : connectionString;
        optionBuilder.UseFileBaseContextDatabase(location: location);
        return optionBuilder;
    }
}
