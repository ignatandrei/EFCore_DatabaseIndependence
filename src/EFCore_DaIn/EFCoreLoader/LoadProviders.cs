using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EFCoreLoader;

public sealed class ProvidersIndex
{
    public DateTimeOffset GeneratedAt { get; set; }
    public string? Configuration { get; set; }
    public List<string>? Runtimes { get; set; }
    public List<ProviderEntry>? Providers { get; set; }

    
    public IEnumerable<RuntimeEntry>  LoadProviders(Version v, string? runtime = null)
    {
        if(runtime == null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                runtime = "linux-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                runtime = "win-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                runtime = "osx-x64";
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS platform.");
            }
        }
        if (Providers == null) yield break;
        foreach (var provider in Providers)
        {
            if (v.Equals(provider.VersionEF))
            {
                var runtimeEntry = provider.Runtimes?.Find(r => r.Rid == runtime);
                if (runtimeEntry == null) continue;
                yield return runtimeEntry;
            }

        }
        yield break;
    }
    public Version[] VersionsEFCore()
    {
        if(Providers == null) return Array.Empty<Version>();
        var versions = new HashSet<Version>();
        foreach(var provider in Providers)
        {
            if(provider.VersionEF != null)
            {
                versions.Add(provider.VersionEF);
            }
        }
        return [.. versions];
    }
}

public  sealed class ProviderEntry
{
    public string? Name { get; set; }
    public string? Project { get; set; }
    public List<RuntimeEntry>? Runtimes { get; set; }
    public Version? VersionEF
    {
        get
        {
            if(Project == null) return null;
            var arrData= Project.Split('/','\\');
            foreach(var item in arrData)
            {
                if(Version.TryParse(item, out var version))
                {
                    return version;
                }

            }
            return null;
        }
    }
}

public sealed class RuntimeEntry
{
    public string? Rid { get; set; }
    public string? Zip { get; set; }

    public string? Name
    {
        get
        {
            if(Rid == null) return null;
            if(Zip == null) return null;
            return Zip.Replace(Rid,"")
                .Replace(".zip","")
                .Replace(@"\", "")
                .Replace("/", "")
                ;
        }
    }
}
