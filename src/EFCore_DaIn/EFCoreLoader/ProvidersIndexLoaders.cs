using EFCore_DaInLoader;
using System.IO.Compression;
using System.Text.Json;

namespace EFCore_DaInLoader;

public class ProvidersIndexLoaders(string url)
{
    //public static async Task<ProvidersIndex?> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    //{
    //    await using var stream = System.IO.File.OpenRead(filePath);
    //    var options = new JsonSerializerOptions
    //    {
    //        PropertyNameCaseInsensitive = true,
    //        AllowTrailingCommas = true
    //    };
    //    var result = await JsonSerializer.DeserializeAsync<ProvidersIndex>(stream, options, cancellationToken).ConfigureAwait(false);
    //    return result;
    //}
    static string? _cachePluginDirectory;
    private static string PluginsDirectory
    {
        get
        {
            if (_cachePluginDirectory is not null)
            {
                return _cachePluginDirectory;
            }
            _cachePluginDirectory= Path.Combine(AppContext.BaseDirectory, "plugins");
            if (!Directory.Exists(_cachePluginDirectory))
            {
                Directory.CreateDirectory(_cachePluginDirectory);
            }
            return _cachePluginDirectory;

        }
    }
    public async Task<string> SaveProviderFromUrl(RuntimeEntry runtimeEntry,Version v, CancellationToken cancellationToken = default)
    {
        string version=v.ToString();
        string providerDirectory = Path.Combine(PluginsDirectory, version, runtimeEntry.Name!);
        if (!Directory.Exists(providerDirectory))
        {
            Directory.CreateDirectory(providerDirectory);
        }

        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(url);
        var requestedProvider = existingProviders.FirstOrDefault(p => VersionFromProvider(p) == v);
        if (requestedProvider == null)
        {
            throw new InvalidOperationException($"No provider found for version {v}");
        }

        try
        {
            var zipUrl =requestedProvider + (runtimeEntry.Zip??"");
            zipUrl = zipUrl.Replace("\\", "/");
            zipUrl = zipUrl.Replace("//", "/");
            Console.WriteLine("Downloading provider zip file from: " + zipUrl);
            var bytes = await httpClient.GetByteArrayAsync(zipUrl, cancellationToken).ConfigureAwait(false);
            var ms = new MemoryStream(bytes);
            var fileZip = Path.Combine(PluginsDirectory,version, runtimeEntry.Zip!);   
            await   File.WriteAllBytesAsync(fileZip, bytes, cancellationToken).ConfigureAwait(false);
            Console.WriteLine("Downloaded provider zip file to: " + fileZip);
            ZipArchive z = new(ms);
            await z.ExtractToDirectoryAsync(Path.Combine(providerDirectory,runtimeEntry.Rid!), true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            httpClient.Dispose(); 
        }
        return providerDirectory;
    }
    static string[] existingProviders = [
        "EFCore_DatabaseIndependence/providers10.0.11/",
        "EFCore_DatabaseIndependence/providers11.0.0/",
    ];
    private Version VersionFromProvider(string provider)
    {
        string[] parts = provider.Split('/');
        var v= parts[1].Replace("providers", "");
        return Version.Parse(v);
    }   
    public async Task<ProvidersVersion> LoadFromUrlAsync(CancellationToken cancellationToken = default)
    {
        ProvidersVersion providersIndexes = new();
        foreach (var provider in existingProviders)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
            try
            {

                var urlJson = provider + "providers.json";
                Console.WriteLine("Loading providers index from: " + urlJson);
                using var resp = await httpClient.GetAsync(urlJson, cancellationToken).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var result = await JsonSerializer.DeserializeAsync<ProvidersIndex>(stream, options, cancellationToken).ConfigureAwait(false);
                if (result is not null)
                {
                    providersIndexes.Add(VersionFromProvider(provider), result);
                }
            }
            finally
            {
                httpClient.Dispose();
            }

        }
        return  providersIndexes ;
    }
}