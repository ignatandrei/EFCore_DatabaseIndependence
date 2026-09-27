using EFCore_DaIn;
using System.Text.Json;

namespace EFCore_Dain_Settings;

public class DainEF_Data : IDainEF_Data
{
    public string PluginName { get; set; }=string.Empty;
    public string ConnectionString { get ; set ; }=string.Empty;
    public string? DatabaseName { get ; set ; }= string.Empty;
}

public class DainEF_Data_CR(string? pluginDir = null) : IDainEF_Data_CR   
{
    
    public async Task<IDainEF_Data?> Retrieve()
    {
        var file = FileToStore(pluginDir);
        if (!File.Exists(file)) return null;
        var content = await File.ReadAllTextAsync(file);
        var dain= JsonSerializer.Deserialize<DainEF_Data>(content);
        return dain;
    }

    private string FileToStore(string? pluginFolder)
    {
        pluginFolder ??= Path.Combine(AppContext.BaseDirectory, "plugins");
        string file = Path.Combine(pluginFolder, "settings.json");
        return file;
    }
    public async Task<bool> Save(IDainEF_Data dainEF_Data)
    {
        try
        {
            DainEF_Data dainEF = (DainEF_Data)dainEF_Data;
            var json = JsonSerializer.Serialize(dainEF);
            await File.WriteAllTextAsync(FileToStore(pluginDir), json);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
        
    }
}