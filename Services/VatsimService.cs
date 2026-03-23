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
    private Dictionary<string, VatsimPilot> _pilotCache = new();

    // US/Canada bounding box
    private const double BoxMinLat = 15.0;
    private const double BoxMaxLat = 75.0;
    private const double BoxMinLon = -170.0;
    private const double BoxMaxLon = -50.0;

    // NAT airspace box
    private const double NatMinLat = 28.0;
    private const double NatMaxLat = 75.0;
    private const double NatMinLon = -60.0;
    private const double NatMaxLon = -15.0;

    // Canadian border airports
    private static readonly HashSet<string> CanadianAirports = new()
    {
        "CYVR", "CYYC", "CYEG", "CYWG", "CYYZ", "CYUL", "CYQB",
        "CYOW", "CYYT", "CYQX", "CYHZ", "CYFC", "CYSJ", "CYXE",
        "CYRQ", "CYXU", "CYKF", "CYAM", "CYTS", "CYDF"
    };

    public event EventHandler<List<VatsimPilot>>? PilotsUpdated;
    public List<VatsimPilot> CurrentPilots { get; private set; } = new();
    public bool IsRunning { get; private set; }

    public void Start()
    {
        IsRunning = true;
        _cts = new CancellationTokenSource();

        // Fetch immediately
        _ = FetchAsync(_cts.Token);

        // Always skip to 2 minutes from now, rounded to clean boundary
        var now = DateTime.UtcNow;
        var nextMinute = now.AddSeconds(60 - now.Second).AddMilliseconds(-now.Millisecond);
        var delay = nextMinute.AddSeconds(60) - now;

        _timer = new Timer(_ => _ = FetchAsync(_cts.Token),
            null, delay, TimeSpan.FromSeconds(60));
    }

    public void Stop()
    {
        IsRunning = false;
        _cts.Cancel();
        _timer?.Dispose();
        _timer = null;
    }

    private async Task FetchAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(VatsimUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var rawPilots = Parse(json);
            var merged = MergeWithCache(rawPilots);

            CurrentPilots = merged;
            PilotsUpdated?.Invoke(this, merged);
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

    private List<VatsimPilot> MergeWithCache(List<VatsimPilot> incoming)
    {
        var newCache = new Dictionary<string, VatsimPilot>();

        foreach (var pilot in incoming)
        {
            if (_pilotCache.TryGetValue(pilot.Callsign, out var existing))
            {
                // Update existing pilot, preserving speed history and parsed route
                existing.Lat = pilot.Lat;
                existing.Lon = pilot.Lon;
                existing.Altitude = pilot.Altitude;
                existing.GroundSpeed = pilot.GroundSpeed;
                existing.Heading = pilot.Heading;
                existing.AircraftType = pilot.AircraftType;
                existing.Departure = pilot.Departure;
                existing.Arrival = pilot.Arrival;
                existing.Route = pilot.Route;
                existing.RecordSpeed(pilot.GroundSpeed);
                newCache[pilot.Callsign] = existing;
            }
            else
            {
                // New pilot — seed speed history
                pilot.RecordSpeed(pilot.GroundSpeed);
                newCache[pilot.Callsign] = pilot;
            }
        }

        _pilotCache = newCache;
        return newCache.Values.ToList();
    }

    private static bool IsInBox(double lat, double lon,
        double minLat, double maxLat, double minLon, double maxLon)
        => lat >= minLat && lat <= maxLat && lon >= minLon && lon <= maxLon;

    private static bool IsUsOrCanadianAirport(string icao)
    {
        if (string.IsNullOrEmpty(icao)) return false;
        if (icao.StartsWith("K", StringComparison.OrdinalIgnoreCase)) return true;
        if (icao.StartsWith("PA", StringComparison.OrdinalIgnoreCase)) return true;
        if (icao.StartsWith("PH", StringComparison.OrdinalIgnoreCase)) return true;
        if (icao.StartsWith("PJ", StringComparison.OrdinalIgnoreCase)) return true;
        if (icao.StartsWith("TJ", StringComparison.OrdinalIgnoreCase)) return true;
        if (icao.StartsWith("TK", StringComparison.OrdinalIgnoreCase)) return true;
        if (icao.StartsWith("TI", StringComparison.OrdinalIgnoreCase)) return true;
        if (CanadianAirports.Contains(icao.ToUpperInvariant())) return true;
        return false;
    }

    private static bool ShouldIncludePilot(double lat, double lon,
        string arrival, string departure)
    {
        if (IsUsOrCanadianAirport(arrival)) return true;
        if (IsInBox(lat, lon, BoxMinLat, BoxMaxLat, BoxMinLon, BoxMaxLon))
            return true;
        if (IsInBox(lat, lon, NatMinLat, NatMaxLat, NatMinLon, NatMaxLon) &&
            IsUsOrCanadianAirport(departure))
            return true;
        return false;
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

                var lat = p.TryGetProperty("latitude", out var latProp)
                    ? latProp.GetDouble() : 0;
                var lon = p.TryGetProperty("longitude", out var lonProp)
                    ? lonProp.GetDouble() : 0;

                string arrival = string.Empty;
                string departure = string.Empty;
                string aircraftType = string.Empty;
                string route = string.Empty;

                if (p.TryGetProperty("flight_plan", out var fp) &&
                    fp.ValueKind == JsonValueKind.Object)
                {
                    arrival = fp.TryGetProperty("arrival", out var arr)
                        ? arr.GetString() ?? string.Empty : string.Empty;
                    departure = fp.TryGetProperty("departure", out var dep)
                        ? dep.GetString() ?? string.Empty : string.Empty;
                    aircraftType = fp.TryGetProperty("aircraft_short", out var ac)
                        ? ac.GetString() ?? string.Empty : string.Empty;
                    route = fp.TryGetProperty("route", out var rt)
                        ? rt.GetString() ?? string.Empty : string.Empty;
                }

                if (!ShouldIncludePilot(lat, lon, arrival, departure))
                    continue;

                var pilot = new VatsimPilot
                {
                    Callsign = p.TryGetProperty("callsign", out var cs)
                        ? cs.GetString() ?? string.Empty : string.Empty,
                    Lat = lat,
                    Lon = lon,
                    Altitude = p.TryGetProperty("altitude", out var alt)
                        ? alt.GetInt32() : 0,
                    GroundSpeed = gs,
                    Heading = p.TryGetProperty("heading", out var hdg)
                        ? hdg.GetInt32() : 0,
                    AircraftType = aircraftType,
                    Departure = departure,
                    Arrival = arrival,
                    Route = route
                };

                result.Add(pilot);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"VatsimService: skipping pilot — {ex.Message}");
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"VatsimService: parsed {result.Count} relevant pilots");
        return result;
    }

    public void Dispose()
    {
        Stop();
    }
}