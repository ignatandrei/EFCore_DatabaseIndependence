using EFCore_DaIn;
using McMaster.NETCore.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Runtime.InteropServices;

namespace EFCore_DaIn_10;

public static class EfCorePluginSharedTypes_10
{
    public static IReadOnlyCollection<Type> Types { get; } =
    [
        typeof(IDatabasePlugin),
        typeof(IEFCore_DatabasePlugin),
        typeof(IEFCore_DatabasePlugin_10),
        typeof(DbContext),
        typeof(DbContextOptions),
        typeof(DbContextOptionsBuilder),
        typeof(IDbContextOptions)
    ];
}

public sealed class EfCorePluginLoadResult_10 
{
    public EfCorePluginLoadResult_10(IReadOnlyList<IEFCore_DatabasePlugin_10> plugins)
    {
        Plugins = plugins;
    }

    public IReadOnlyList<IEFCore_DatabasePlugin_10> Plugins { get; }

}

public static class EfCorePluginLoader_10
{
    public static string GetCurrentRuntimeFolderName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-x64";
        }

        throw new PlatformNotSupportedException("Unsupported OS platform.");
    }

    public static IReadOnlyList<string> DiscoverPluginAssemblies(string pluginsRootPath, string? runtimeFolderName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginsRootPath);

        if (!Directory.Exists(pluginsRootPath))
        {
            return Array.Empty<string>();
        }
        pluginsRootPath= Path.Combine(pluginsRootPath, "10.0.11");
        if (!Directory.Exists(pluginsRootPath))
        {
            return Array.Empty<string>();
        }
        runtimeFolderName ??= GetCurrentRuntimeFolderName();

        var pluginAssemblies = Directory
            .EnumerateDirectories(pluginsRootPath)
            .Select(providerDirectory => Path.Combine(providerDirectory, runtimeFolderName))
            .Where(Directory.Exists)
            .SelectMany(runtimeDirectory => Directory.EnumerateFiles(runtimeDirectory, "DainEF.Providers.*.dll", SearchOption.TopDirectoryOnly))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return pluginAssemblies;
    }

    public static PluginLoader CreateLoader(string pluginAssemblyPath, bool isUnloadable = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginAssemblyPath);

        return PluginLoader.CreateFromAssemblyFile(
            pluginAssemblyPath,
            sharedTypes: [.. EfCorePluginSharedTypes_10.Types],
            isUnloadable: isUnloadable);
    }

    public static EfCorePluginLoadResult_10 LoadFromAssembly(string pluginAssemblyPath, bool isUnloadable = false)
    {
        using var loader = CreateLoader(pluginAssemblyPath, isUnloadable);
        var assembly = loader.LoadDefaultAssembly();

        var plugins = assembly
            .GetTypes()
            .Where(t => typeof(IEFCore_DatabasePlugin_10).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IEFCore_DatabasePlugin_10)Activator.CreateInstance(t)!)
            .ToArray();

        return new EfCorePluginLoadResult_10(plugins);
    }
}
