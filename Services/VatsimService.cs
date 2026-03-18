using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using vTFMS.Models;

namespace vTFMS.Services;

public class VatsimService : IVatsimService, IDisposable
{
    private static readonly HttpClient _httpClient = new();
    private const string VatsimUrl =
        "https://data.vatsim.net/v3/vatsim-data.json";
    private Timer? _timer;
    private CancellationTokenSource _cts = new();

    public event EventHandler<List<VatsimPilot>>? PilotsUpdated;
    public List<VatsimPilot> CurrentPilots { get; private set; } = new();

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _timer = new Timer(_ => _ = FetchAsync(_cts.Token),
            null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    public void Stop()
    {
        _cts.Cancel();
        _timer?.Dispose();
        _timer = null;
    }

    private async Task FetchAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient
                .GetAsync(VatsimUrl, ct);

            response.EnsureSuccessStatusCode();

            var json = await response.Content
                .ReadAsStringAsync(ct);

            var pilots = Parse(json);

            CurrentPilots = pilots;
            PilotsUpdated?.Invoke(this, pilots);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine(
                "VatsimService: fetch cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"VatsimService: fetch failed — {ex.Message}");
        }
    }

    private static List<VatsimPilot> Parse(string json)
    {
        var result = new List<VatsimPilot>();

        var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("pilots", out var pilots))
            return result;

        foreach (var p in pilots.EnumerateArray())
        {
            try
            {
                var gs = p.TryGetProperty("groundspeed", out var gsProp)
                    ? gsProp.GetInt32() : 0;

                if (gs <= 35) continue;

                var pilot = new VatsimPilot
                {
                    Callsign = p.TryGetProperty("callsign", out var cs)
                        ? cs.GetString() ?? string.Empty : string.Empty,
                    Lat = p.TryGetProperty("latitude", out var lat)
                        ? lat.GetDouble() : 0,
                    Lon = p.TryGetProperty("longitude", out var lon)
                        ? lon.GetDouble() : 0,
                    Altitude = p.TryGetProperty("altitude", out var alt)
                        ? alt.GetInt32() : 0,
                    GroundSpeed = gs,
                    Heading = p.TryGetProperty("heading", out var hdg)
                        ? hdg.GetInt32() : 0,
                };

                if (p.TryGetProperty("flight_plan", out var fp) &&
                    fp.ValueKind == JsonValueKind.Object)
                {
                    pilot.AircraftType = fp.TryGetProperty("aircraft_short",
                        out var ac) ? ac.GetString() ?? string.Empty
                        : string.Empty;
                    pilot.Departure = fp.TryGetProperty("departure",
                        out var dep) ? dep.GetString() ?? string.Empty
                        : string.Empty;
                    pilot.Arrival = fp.TryGetProperty("arrival",
                        out var arr) ? arr.GetString() ?? string.Empty
                        : string.Empty;
                }

                result.Add(pilot);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"VatsimService: skipping pilot — {ex.Message}");
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"VatsimService: parsed {result.Count} airborne pilots");
        return result;
    }

    public void Dispose()
    {
        Stop();
    }
}