using System.Threading.Tasks;
#nullable enable
namespace EFCore_DaIn
{

    public interface IDainEF_Data
    {
        string PluginName { get; set; }
        string ConnectionString { get; set; }
        string? DatabaseName { get; set; }

    }
    public interface IDainEF_Data_CR
    {
        Task<bool> Save(IDainEF_Data dainEF_Data);

        Task<IDainEF_Data?> Retrieve();
    }
}
#nullable disable