# EFCore Database Independence

Load EF Core database providers as plugins instead of taking a compile-time dependency on every provider.

## Packages

- `EFCore_DaIn` targets `netstandard2.0` and contains the shared `IDatabasePlugin` contract.
- `EFCore_DaIn_10` targets `net10.0`, contains the EF Core 10 plugin contracts and `EfCorePluginLoader_10`, and depends on `EFCore_DaIn`.

The provider implementations in this repository are built separately for SQLite, PostgreSQL, SQL Server, and MongoDB. They are plugin assemblies, not dependencies that must be referenced by the host application.

## Load plugins from the published provider index

`ProvidersIndexLoaders` reads `providers.json`, selects entries for the current OS runtime, downloads each provider ZIP, and extracts it below `AppContext.BaseDirectory\plugins`.

```csharp
using EFCoreLoader;
using EFCore_DaIn_10;

var indexLoader = new ProvidersIndexLoaders("https://ignatandrei.github.io/");
var index = await indexLoader.LoadFromUrlAsync();

foreach (var version in index?.VersionsEFCore() ?? [])
{
    foreach (var runtime in index.LoadProviders(version))
    {
        var providerDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "plugins",
            version.ToString(),
            runtime.Name!);

        var extractedDirectory = Path.Combine(providerDirectory, runtime.Rid!);
        if (!Directory.Exists(extractedDirectory))
        {
            await indexLoader.SaveProviderFromUrl(runtime, version);
        }
    }
}

var assemblies = EfCorePluginLoader_10.DiscoverPluginAssemblies(
    Path.Combine(AppContext.BaseDirectory, "plugins"));

foreach (var assemblyPath in assemblies)
{
    foreach (var plugin in EfCorePluginLoader_10.LoadFromAssembly(assemblyPath))
    {
        Console.WriteLine($"{plugin.ProviderName}: {plugin.Description}");
    }
}
```

The index URL must expose `EFCore_DatabaseIndependence/providers10.0.11/providers.json` and the ZIP paths listed by that JSON. The current loader chooses `win-x64`, `linux-x64`, or `osx-x64`.

## Load plugins already on disk

If provider ZIPs have already been extracted, no network access is required. The expected directory layout is:

```text
plugins\
  10.0.11\
    DainEF.Providers.Sqlite\
      win-x64\
        DainEF.Providers.Sqlite.dll
```

Discover and load the assemblies directly:

```csharp
var assemblies = EfCorePluginLoader_10.DiscoverPluginAssemblies(
    @"D:\my-app\plugins",
    runtimeFolderName: "win-x64");

foreach (var assemblyPath in assemblies)
{
    var plugins = EfCorePluginLoader_10.LoadFromAssembly(
        assemblyPath,
        isUnloadable: false);
}
```

For a single known DLL, call `LoadFromAssembly` with its full path. `CreateLoader` is available when the host needs to control the `PluginLoader` lifetime.

## Configure a DbContext

Each loaded plugin implements `IEFCore_DatabasePlugin_10`:

```csharp
var options = plugin
    .GenerateDbContextOptionsBuilder<MyDbContext>(connectionString, "MyDatabase")
    .Options;

await using var db = new MyDbContext(options);
```

Alternatively, apply the provider to an existing options builder with `plugin.UsePlugin(...)`.

## Build and publish

From the repository root:

```powershell
dotnet pack .\src\EFCore_DaIn\EFCore_DaIn\EFCore_DaIn.csproj -c Release -o .\artifacts\packages
dotnet pack .\src\EFCore_DaIn\10.0.11\EFCore_DaIn_10\EFCore_DaIn_10.csproj -c Release -o .\artifacts\packages
dotnet nuget push .\artifacts\packages\EFCore_DaIn.10.0.11.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push .\artifacts\packages\EFCore_DaIn_10.10.0.11.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

Use a NuGet API key with push permission, and publish `EFCore_DaIn` before `EFCore_DaIn_10` because the latter references the former.
