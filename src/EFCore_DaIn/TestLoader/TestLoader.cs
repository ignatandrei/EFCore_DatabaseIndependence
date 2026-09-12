using EFCoreLoader;
//[assembly: CaptureConsole]
namespace TestLoader;

public class TestLoader
{
    [Fact]
    public async Task TestLoad10()
    {
        ProvidersIndexLoaders loaders = new ("http://localhost:53732");
        var result=await loaders.LoadFromUrlAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result != null);
        Assert.True(result.Providers?.Count > 0);
        var versions = result.VersionsEFCore();
        Assert.True(versions != null);
        Assert.True(versions?.Length > 0);
        foreach (var version in versions)
        {
            var runtimeOnCurrentOS = result.LoadProviders(version);
            Assert.NotNull(runtimeOnCurrentOS);
            foreach (var runtime in runtimeOnCurrentOS)
            {
                await loaders.SaveProviderFromUrl(runtime, cancellationToken: TestContext.Current.CancellationToken);
            }
        }
    }
}


