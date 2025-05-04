using l2l_aggregator.Models;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database.Repositories.Interfaces
{
    public interface IConfigRepository
    {
        Task<string?> GetConfigValueAsync(string key);
        Task SetConfigValueAsync(string key, string value);
        Task SaveScannerDeviceAsync(ScannerDevice scanner);
        Task<ScannerDevice?> LoadScannerDeviceAsync();
    }
}
