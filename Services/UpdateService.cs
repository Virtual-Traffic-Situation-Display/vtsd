using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace vTFMS.Services;

public class UpdateService : IUpdateService
{
    private readonly UpdateManager _updateManager;

    public UpdateService()
    {
        _updateManager = new UpdateManager(
            new GithubSource(
                repoUrl: "https://github.com/Virtual-Traffic-Situation-Display/vtsd",
                accessToken: null,   // public repo — no token needed
                prerelease: true    // set to true if you want beta releases offered
            )
        );
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        // If not installed via Velopack (e.g. running from dotnet run),
        // IsInstalled will be false and we skip the check gracefully.
        if (!_updateManager.IsInstalled)
            return null;

        return await _updateManager.CheckForUpdatesAsync();
    }

    public async Task DownloadAndApplyUpdateAsync(UpdateInfo updateInfo)
    {
        await _updateManager.DownloadUpdatesAsync(updateInfo);

        // Applies the update and restarts the app. Does not return.
        _updateManager.ApplyUpdatesAndRestart(updateInfo);
    }
}