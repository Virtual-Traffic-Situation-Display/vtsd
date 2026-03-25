using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using vTFMS.Models;

namespace vTFMS.Services;

/// <summary>
/// Service for loading and querying aviation map data including airports,
/// navaids, waypoints, airways, boundaries, and ARTCC sectors.
///
/// Split across partial class files for maintainability:
///   MapDataService.cs             — Core nav data loading, lookups, resource helpers
///   MapDataService.Boundaries.cs  — State, country, TRACON, ARTCC boundary loading
///   MapDataService.Routes.cs      — Airway loading, route resolution, altitude estimation
///   MapDataService.Sectors.cs     — ARTCC sector loading and spatial queries
/// </summary>
public partial class MapDataService : IMapDataService
{
    private static Assembly Assembly => Assembly.GetExecutingAssembly();

    private readonly Dictionary<string, Airport> _airports = new();
    private readonly Dictionary<string, Navaid> _navaids = new();
    private readonly Dictionary<string, Waypoint> _waypoints = new();

    // =========================================================================
    // Constructor
    // =========================================================================

    public MapDataService()
    {
        LoadAirports();
        LoadNavaids();
        LoadWaypoints();
    }

    // =========================================================================
    // Nav data loading — airports, navaids, waypoints
    // =========================================================================

    private void LoadAirports()
    {
        foreach (var fields in ReadCsv("APT_BASE.csv"))
        {
            try
            {
                if (fields.Length < 25) continue;
                if (!double.TryParse(fields[19], out double lat)) continue;
                if (!double.TryParse(fields[24], out double lon)) continue;

                var identifier = fields[4].Trim();
                if (string.IsNullOrWhiteSpace(identifier)) continue;

                _airports[identifier.ToUpperInvariant()] = new Airport
                {
                    Identifier = identifier,
                    Name = fields[12].Trim(),
                    Type = fields[2].Trim(),
                    Lat = lat,
                    Lon = lon
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MapDataService: skipping airport row — {ex.Message}");
            }
        }
        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {_airports.Count} airports");
    }

    private void LoadNavaids()
    {
        foreach (var fields in ReadCsv("NAV_BASE.csv"))
        {
            try
            {
                if (fields.Length < 32) continue;
                if (!double.TryParse(fields[26], out double lat)) continue;
                if (!double.TryParse(fields[31], out double lon)) continue;

                var identifier = fields[1].Trim();
                if (string.IsNullOrWhiteSpace(identifier)) continue;

                _navaids[identifier.ToUpperInvariant()] = new Navaid
                {
                    Identifier = identifier,
                    Name = fields[7].Trim(),
                    Type = fields[2].Trim(),
                    Lat = lat,
                    Lon = lon
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MapDataService: skipping navaid row — {ex.Message}");
            }
        }
        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {_navaids.Count} navaids");
    }

    private void LoadWaypoints()
    {
        foreach (var fields in ReadCsv("FIX_BASE.csv"))
        {
            try
            {
                if (fields.Length < 15) continue;
                if (!double.TryParse(fields[9], out double lat)) continue;
                if (!double.TryParse(fields[14], out double lon)) continue;

                var identifier = fields[1].Trim();
                if (string.IsNullOrWhiteSpace(identifier)) continue;

                _waypoints[identifier.ToUpperInvariant()] = new Waypoint
                {
                    Identifier = identifier,
                    Lat = lat,
                    Lon = lon
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MapDataService: skipping waypoint row — {ex.Message}");
            }
        }
        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {_waypoints.Count} waypoints");
    }

    // =========================================================================
    // Nav data lookups
    // =========================================================================

    public Airport? FindAirport(string identifier)
    {
        _airports.TryGetValue(identifier.ToUpperInvariant(), out var airport);
        return airport;
    }

    public Navaid? FindNavaid(string identifier)
    {
        _navaids.TryGetValue(identifier.ToUpperInvariant(), out var navaid);
        return navaid;
    }

    public Waypoint? FindWaypoint(string identifier)
    {
        _waypoints.TryGetValue(identifier.ToUpperInvariant(), out var waypoint);
        return waypoint;
    }

    // =========================================================================
    // Embedded resource helpers (CSV / file reading)
    // =========================================================================

    private static string? FindResource(string fileName)
    {
        return Assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static StreamReader? OpenResource(string fileName)
    {
        var resourceName = FindResource(fileName);
        if (resourceName == null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"MapDataService: resource not found — {fileName}");
            return null;
        }

        var stream = Assembly.GetManifestResourceStream(resourceName);
        return stream == null ? null : new StreamReader(stream);
    }

    private static IEnumerable<string[]> ReadCsv(string fileName)
    {
        using var reader = OpenResource(fileName);
        if (reader == null) yield break;

        reader.ReadLine();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            yield return SplitCsvLine(line);
        }
    }

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
                current.Append(c);
        }

        fields.Add(current.ToString().Trim());
        return fields.ToArray();
    }
}