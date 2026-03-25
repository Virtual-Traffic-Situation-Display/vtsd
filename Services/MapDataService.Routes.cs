using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using vTFMS.Models;

namespace vTFMS.Services;

public partial class MapDataService
{
    // =========================================================================
    // Airways — lazy loaded from disk on first request
    // =========================================================================

    private Dictionary<string, Airway>? _airways = null;
    private readonly object _airwayLock = new();

    private Dictionary<string, Airway> EnsureAirwaysLoaded()
    {
        if (_airways != null) return _airways;

        lock (_airwayLock)
        {
            if (_airways != null) return _airways;

            var result = new Dictionary<string, Airway>(
                StringComparer.OrdinalIgnoreCase);

            var filePath = Path.Combine(
                AppContext.BaseDirectory, "Data", "AWY_BASE.csv");

            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine(
                    "MapDataService: AWY_BASE.csv not found");
                _airways = result;
                return _airways;
            }

            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = SplitCsvLine(line);
                if (fields.Length < 8) continue;

                try
                {
                    var type = fields[2].Trim();
                    var id = fields[4].Trim();
                    var waypointString = fields[7].Trim();

                    if (string.IsNullOrWhiteSpace(id) ||
                        string.IsNullOrWhiteSpace(waypointString))
                        continue;

                    var waypoints = waypointString
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    result[id.ToUpperInvariant()] = new Airway
                    {
                        Identifier = id,
                        Type = type,
                        WaypointNames = waypoints
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"MapDataService: skipping airway row — {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"MapDataService: loaded {result.Count} airways");

            _airways = result;
            return _airways;
        }
    }

    // =========================================================================
    // Airway and waypoint lookup
    // =========================================================================

    public Airway? GetAirway(string identifier)
    {
        var airways = EnsureAirwaysLoaded();

        if (!airways.TryGetValue(identifier.ToUpperInvariant(), out var airway))
            return null;

        if (!airway.IsResolved)
        {
            airway.ResolvedPoints = airway.WaypointNames
                .Select(name => ResolveWaypoint(name))
                .ToList();
            airway.IsResolved = true;
        }

        return airway;
    }

    public LatLon? ResolveWaypoint(string identifier)
    {
        var nav = FindNavaid(identifier);
        if (nav != null) return new LatLon(nav.Lat, nav.Lon);

        var wp = FindWaypoint(identifier);
        if (wp != null) return new LatLon(wp.Lat, wp.Lon);

        var apt = FindAirport(identifier);
        if (apt != null) return new LatLon(apt.Lat, apt.Lon);

        return null;
    }

    // =========================================================================
    // Route resolution
    // =========================================================================

    public List<LatLon> ResolveRoute(string departure,
        string route, string arrival)
    {
        var result = new List<LatLon>();

        var dep = FindAirport(departure);
        if (dep != null)
            result.Add(new LatLon(dep.Lat, dep.Lon));

        var tokens = route
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token == "DCT") continue;

            if (IsAirwayToken(token))
            {
                string? entryName = i > 0 ? tokens[i - 1] : null;
                string? exitName = i < tokens.Count - 1
                    ? tokens[i + 1] : null;

                if (entryName != null && exitName != null)
                {
                    var airwayPoints = ResolveAirwaySegment(
                        token, entryName, exitName);
                    result.AddRange(airwayPoints);
                }
                continue;
            }

            var coord = TryParseCoordWaypoint(token);
            if (coord != null) { result.Add(coord); continue; }

            var airport = FindAirport(token);
            if (airport != null)
            {
                result.Add(new LatLon(airport.Lat, airport.Lon));
                continue;
            }

            var navaid = FindNavaid(token);
            if (navaid != null)
            {
                result.Add(new LatLon(navaid.Lat, navaid.Lon));
                continue;
            }

            var waypoint = FindWaypoint(token);
            if (waypoint != null)
                result.Add(new LatLon(waypoint.Lat, waypoint.Lon));
        }

        var arr = FindAirport(arrival);
        if (arr != null)
            result.Add(new LatLon(arr.Lat, arr.Lon));

        return result;
    }

    private List<LatLon> ResolveAirwaySegment(
        string airwayId, string entryName, string exitName)
    {
        var result = new List<LatLon>();

        var airway = GetAirway(airwayId);
        if (airway == null) return result;

        var names = airway.WaypointNames;

        int entryIdx = names.FindIndex(n =>
            n.Equals(entryName, StringComparison.OrdinalIgnoreCase));
        int exitIdx = names.FindIndex(n =>
            n.Equals(exitName, StringComparison.OrdinalIgnoreCase));

        if (entryIdx < 0 || exitIdx < 0) return result;

        int step = entryIdx < exitIdx ? 1 : -1;

        for (int i = entryIdx + step;
            step > 0 ? i <= exitIdx : i >= exitIdx;
            i += step)
        {
            if (i < 0 || i >= airway.ResolvedPoints.Count) break;
            var pt = airway.ResolvedPoints[i];
            if (pt != null)
                result.Add(pt);
        }

        return result;
    }

    // =========================================================================
    // Route parsing helpers
    // =========================================================================

    private static bool IsAirwayToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        if (token == "DCT") return false;

        int i = 0;
        while (i < token.Length && char.IsLetter(token[i])) i++;
        if (i == 0 || i >= token.Length) return false;
        while (i < token.Length && char.IsDigit(token[i])) i++;
        return i == token.Length;
    }

    private static LatLon? TryParseCoordWaypoint(string token)
    {
        int nIdx = token.IndexOfAny(new[] { 'N', 'S' });
        int ewIdx = token.IndexOfAny(new[] { 'E', 'W' });

        if (nIdx <= 0 || ewIdx <= nIdx) return null;

        if (!double.TryParse(token[..nIdx], out double lat))
            return null;
        if (!double.TryParse(token[(nIdx + 1)..ewIdx], out double lon))
            return null;

        if (token[nIdx] == 'S') lat = -lat;
        if (token[ewIdx] == 'W') lon = -lon;

        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            return null;

        return new LatLon(lat, lon);
    }

    // =========================================================================
    // Altitude estimation
    // =========================================================================

    public int EstimateAltitude(VatsimPilot pilot, LatLon projectedPos)
    {
        // If no destination, assume cruise altitude
        if (string.IsNullOrWhiteSpace(pilot.Arrival))
            return pilot.Altitude;

        var destAirport = FindAirport(pilot.Arrival);
        if (destAirport == null)
            return pilot.Altitude;

        // Distance from projected position to destination in nm
        double distToDestNm = RouteProjector.DistanceNm(
            projectedPos.Lat, projectedPos.Lon,
            destAirport.Lat, destAirport.Lon);

        // Top of descent distance: (cruiseAlt / 1000) * 3 nm
        double todDistNm = (pilot.Altitude / 1000.0) * 3.0;

        if (distToDestNm >= todDistNm)
        {
            // Still at cruise altitude
            return pilot.Altitude;
        }
        else
        {
            // Descending: 1000ft per 3nm
            int estimatedAlt = (int)(distToDestNm / 3.0 * 1000.0);
            return Math.Max(estimatedAlt, 0);
        }
    }
}