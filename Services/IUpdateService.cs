using System.Threading.Tasks;
using Velopack.Sources;

namespace vTFMS.Services;

public interface IUpdateService
{
    Task<Velopack.UpdateInfo?> CheckForUpdatesAsync();
    Task DownloadAndApplyUpdateAsync(Velopack.UpdateInfo updateInfo);
}