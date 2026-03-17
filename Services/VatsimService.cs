using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using vTFMS.Models;

namespace vTFMS.Services;

public class VatsimService : IVatsimService, IDisposable
{
    private static readonly HttpClient _httpClient = new();
    private const string VatsimUrl =
        "https://data.vatsim.net/v3/vatsim-data.json";
    private Timer? _timer;

    public event EventHandler<List<VatsimPilot>>? PilotsUpdated;
    public List<VatsimPilot> CurrentPilots { get; private set; } = new();

    public void Start()
    {
        // Fetch immediately then every 60 seconds
        _timer = new Timer(_ => FetchAsync(),
            null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async void FetchAsync()
    {
        try
        {
            var json = await _httpClient.GetStringAsync(VatsimUrl);
            var pilots = Parse(json);
            CurrentPilots = pilots;
            PilotsUpdated?.Invoke(this, pilots);
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

                // Only airborne
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

                // Flight plan fields
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
        _httpClient.Dispose();
    }
}