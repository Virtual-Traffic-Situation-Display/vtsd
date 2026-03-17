using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace vTFMS.Services;

public class WeatherService : IWeatherService, IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private Timer? _timer;

    public event EventHandler? AutoRefreshTriggered;

    private const string WmsUrl =
        "https://mapservices.weather.noaa.gov/eventdriven/services/" +
        "radar/radar_base_reflectivity/MapServer/WMSServer";

    public void Start()
    {
        _timer = new Timer(_ => AutoRefreshTriggered?.Invoke(this, EventArgs.Empty),
            null, TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public async Task<byte[]?> FetchRadarAsync(
        double minLat, double minLon,
        double maxLat, double maxLon,
        int width, int height)
    {
        try
        {
            int clampedWidth = Math.Min(width, 4096);
            int clampedHeight = Math.Min(height, 4096);

            var url = $"{WmsUrl}?" +
                $"service=WMS&version=1.3.0&request=GetMap" +
                $"&layers=0" +
                $"&styles=" +
                $"&crs=CRS:84" +
                $"&bbox={minLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{minLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{maxLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{maxLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&width={clampedWidth}&height={clampedHeight}" +
                $"&format=image/png" +
                $"&transparent=true";

            System.Diagnostics.Debug.WriteLine(
                $"WeatherService: fetching — {url}");

            var response = await _httpClient
                .GetAsync(url)
                .WaitAsync(TimeSpan.FromSeconds(30));

            response.EnsureSuccessStatusCode();

            var bytes = await response.Content
                .ReadAsByteArrayAsync();

            if (bytes.Length < 1000)
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                System.Diagnostics.Debug.WriteLine(
                    $"WeatherService: small response — {text}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine(
                $"WeatherService: received {bytes.Length} bytes");

            return bytes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WeatherService: fetch failed — {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
    }
}