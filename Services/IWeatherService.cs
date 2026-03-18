using System;
using System.Threading;
using System.Threading.Tasks;

namespace vTFMS.Services;

public interface IWeatherService
{
    event EventHandler? AutoRefreshTriggered;
    void Start();
    void Stop();
    Task<byte[]?> FetchRadarAsync(double minLat, double minLon,
                                   double maxLat, double maxLon,
                                   int width, int height,
                                    CancellationToken ct = default);
}