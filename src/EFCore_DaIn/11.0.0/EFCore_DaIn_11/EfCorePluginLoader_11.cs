using EFCore_DaIn;
using EFCore_Dain_Settings;
using McMaster.NETCore.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Runtime.InteropServices;

namespace EFCore_DaIn_11;

public static class EfCorePluginSharedTypes_11
{
    public static IReadOnlyCollection<Type> Types { get; } =
    [
        typeof(IDatabasePlugin),
        typeof(IEFCore_DatabasePlugin),
        typeof(IEFCore_DatabasePlugin_11),
        typeof(DbContext),
        typeof(DbContextOptions),
        typeof(DbContextOptionsBuilder),
        typeof(IDbContextOptions),
    ];
}

public static class EfCorePluginLoader_11
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

    public static async Task<bool> SaveChosenPlugin(IDainEF_Data dainEF, string? pluginsDirectory = null, IDainEF_Data_CR? cr = null)
    {
        cr ??= new DainEF_Data_CR(pluginsDirectory);
        return await cr.Save(dainEF);
    }

    public static async Task<(IEFCore_DatabasePlugin_11?, IDainEF_Data?)> RetrieveLatestChosenPlugin(string? pluginsDirectory = null, IDainEF_Data_CR? cr = null)
    {
        pluginsDirectory ??= Path.Combine(AppContext.BaseDirectory, "plugins");
        cr ??= new DainEF_Data_CR(pluginsDirectory);
        var data = await cr.Retrieve();
        if (data is null)
        {
            return (null, null);
        }

        foreach (var assembly in DiscoverPluginAssemblies(pluginsDirectory))
        {
            foreach (var plugin in LoadFromAssembly(assembly, isUnloadable: false))
            {
                if (string.Equals(plugin.ProviderName, data.PluginName))
                {
                    return (plugin, data);
                }
            }
        }

        return (null, data);
    }

    public static IReadOnlyList<string> DiscoverPluginAssemblies(string pluginsRootPath, string? runtimeFolderName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginsRootPath);

        var versionDirectory = Path.Combine(pluginsRootPath, "11.0.0");
        if (!Directory.Exists(versionDirectory))
        {
            return Array.Empty<string>();
        }

        runtimeFolderName ??= GetCurrentRuntimeFolderName();

        return Directory
            .EnumerateDirectories(versionDirectory)
            .Select(providerDirectory => Path.Combine(providerDirectory, runtimeFolderName))
            .Where(Directory.Exists)
            .SelectMany(runtimeDirectory => Directory.EnumerateFiles(runtimeDirectory, "DainEF.Providers.*.dll", SearchOption.TopDirectoryOnly))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static PluginLoader CreateLoader(string pluginAssemblyPath, bool isUnloadable = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginAssemblyPath);

        return PluginLoader.CreateFromAssemblyFile(
            pluginAssemblyPath,
            sharedTypes: [.. EfCorePluginSharedTypes_11.Types],
            isUnloadable: isUnloadable);
    }

    public static IReadOnlyList<IEFCore_DatabasePlugin_11> LoadFromAssembly(string pluginAssemblyPath, bool isUnloadable = false)
    {
        using var loader = CreateLoader(pluginAssemblyPath, isUnloadable);
        var assembly = loader.LoadDefaultAssembly();

        return assembly
            .GetTypes()
            .Where(t => typeof(IEFCore_DatabasePlugin_11).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IEFCore_DatabasePlugin_11)Activator.CreateInstance(t)!)
            .ToArray();
    }
}
