using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using vTFMS.Models;

namespace vTFMS.Services;

public class MapDataService : IMapDataService
{
    private static Assembly Assembly => Assembly.GetExecutingAssembly();

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

    private static List<(double Lat, double Lon)> ParseRing(JsonElement ring)
    {
        var points = new List<(double Lat, double Lon)>();
        foreach (var coord in ring.EnumerateArray())
        {
            var lon = coord[0].GetDouble();
            var lat = coord[1].GetDouble();
            points.Add((lat, lon));
        }
        return points;
    }

    public List<StateBoundary> LoadStateBoundaries()
    {
        var result = new List<StateBoundary>();

        using var reader = OpenResource("na-provinces.json");
        if (reader == null) return result;

        try
        {
            var json = reader.ReadToEnd();
            var doc = JsonDocument.Parse(json);
            var features = doc.RootElement.GetProperty("features");

            foreach (var feature in features.EnumerateArray())
            {
                try
                {
                    var properties = feature.GetProperty("properties");

                    // Only keep US states
                    var country = string.Empty;
                    if (properties.TryGetProperty("admin", out var adminProp))
                        country = adminProp.GetString() ?? string.Empty;

                    if (country != "United States of America")
                        continue;

                    var name = string.Empty;
                    if (properties.TryGetProperty("name", out var nameProp))
                        name = nameProp.GetString() ?? string.Empty;

                    var geometry = feature.GetProperty("geometry");
                    var type = geometry.GetProperty("type").GetString();
                    var coordinates = geometry.GetProperty("coordinates");

                    if (type == "Polygon")
                    {
                        var boundary = new StateBoundary
                        {
                            Name = name,
                            Points = ParseRing(coordinates[0])
                        };
                        if (boundary.Points.Count > 0)
                            result.Add(boundary);
                    }
                    else if (type == "MultiPolygon")
                    {
                        foreach (var polygon in coordinates.EnumerateArray())
                        {
                            var boundary = new StateBoundary
                            {
                                Name = name,
                                Points = ParseRing(polygon[0])
                            };
                            if (boundary.Points.Count > 0)
                                result.Add(boundary);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"MapDataService: skipping state feature — {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"MapDataService: failed to load state boundaries — {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {result.Count} state boundaries");
        return result;
    }

    public List<StateBoundary> LoadCountryBoundaries()
    {
        var result = new List<StateBoundary>();

        // Load Canadian provinces from admin-1 file
        using (var reader = OpenResource("na-provinces.json"))
        {
            if (reader != null)
            {
                try
                {
                    var json = reader.ReadToEnd();
                    var doc = JsonDocument.Parse(json);
                    var features = doc.RootElement.GetProperty("features");

                    foreach (var feature in features.EnumerateArray())
                    {
                        try
                        {
                            var properties = feature.GetProperty("properties");

                            var country = string.Empty;
                            if (properties.TryGetProperty("admin", out var adminProp))
                                country = adminProp.GetString() ?? string.Empty;

                            // Only Canada from the provinces file
                            if (country != "Canada") continue;

                            var name = string.Empty;
                            if (properties.TryGetProperty("name", out var nameProp))
                                name = nameProp.GetString() ?? string.Empty;

                            var geometry = feature.GetProperty("geometry");
                            var type = geometry.GetProperty("type").GetString();
                            var coordinates = geometry.GetProperty("coordinates");

                            if (type == "Polygon")
                            {
                                var boundary = new StateBoundary
                                {
                                    Name = name,
                                    Points = ParseRing(coordinates[0])
                                };
                                if (boundary.Points.Count > 0)
                                    result.Add(boundary);
                            }
                            else if (type == "MultiPolygon")
                            {
                                foreach (var polygon in coordinates.EnumerateArray())
                                {
                                    var boundary = new StateBoundary
                                    {
                                        Name = name,
                                        Points = ParseRing(polygon[0])
                                    };
                                    if (boundary.Points.Count > 0)
                                        result.Add(boundary);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"MapDataService: skipping Canada province — {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"MapDataService: failed to load Canada provinces — {ex.Message}");
                }
            }
        }

        // Load Mexico country outline from admin-0 file
        // Load Mexico and Caribbean country outlines from admin-0 file
        using (var reader = OpenResource("na-countries.json"))
        {
            if (reader != null)
            {
                try
                {
                    var json = reader.ReadToEnd();
                    var doc = JsonDocument.Parse(json);
                    var features = doc.RootElement.GetProperty("features");

                    var caribbeanAndMexico = new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase)
            {
                "Mexico",
                "Cuba",
                "Jamaica",
                "Haiti",
                "Dominican Republic",
                "Puerto Rico",
                "Bahamas",
                "Trinidad and Tobago",
                "Barbados",
                "Saint Lucia",
                "Saint Vincent and the Grenadines",
                "Grenada",
                "Antigua and Barbuda",
                "Dominica",
                "Saint Kitts and Nevis",
                "Aruba",
                "Curacao",
                "Sint Maarten",
                "Turks and Caicos Islands",
                "Cayman Islands",
                "British Virgin Islands",
                "U.S. Virgin Islands",
                "Anguilla",
                "Montserrat",
                "Guadeloupe",
                "Martinique",
                "Saint Barthelemy",
                "Saint Martin",
                "Belize",
                "Guatemala",
                "Honduras",
                "El Salvador",
                "Nicaragua",
                "Costa Rica",
                "Panama"
            };

                    foreach (var feature in features.EnumerateArray())
                    {
                        try
                        {
                            var properties = feature.GetProperty("properties");

                            var name = string.Empty;
                            if (properties.TryGetProperty("NAME", out var nameProp) ||
                                properties.TryGetProperty("name", out nameProp))
                                name = nameProp.GetString() ?? string.Empty;

                            if (!caribbeanAndMexico.Contains(name)) continue;

                            var geometry = feature.GetProperty("geometry");
                            var type = geometry.GetProperty("type").GetString();
                            var coordinates = geometry.GetProperty("coordinates");

                            if (type == "Polygon")
                            {
                                var boundary = new StateBoundary
                                {
                                    Name = name,
                                    Points = ParseRing(coordinates[0])
                                };
                                if (boundary.Points.Count > 0)
                                    result.Add(boundary);
                            }
                            else if (type == "MultiPolygon")
                            {
                                foreach (var polygon in coordinates.EnumerateArray())
                                {
                                    var boundary = new StateBoundary
                                    {
                                        Name = name,
                                        Points = ParseRing(polygon[0])
                                    };
                                    if (boundary.Points.Count > 0)
                                        result.Add(boundary);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"MapDataService: skipping Caribbean feature — {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"MapDataService: failed to load Caribbean outlines — {ex.Message}");
                }
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {result.Count} country boundaries");
        return result;
    }

    public Airport? FindAirport(string identifier)
    {
        foreach (var fields in ReadCsv("APT_BASE.csv"))
        {
            try
            {
                if (fields.Length < 25) continue;
                if (!fields[4].Trim().Equals(identifier,
                    StringComparison.OrdinalIgnoreCase)) continue;

                if (!double.TryParse(fields[19], out double lat)) continue;
                if (!double.TryParse(fields[24], out double lon)) continue;

                return new Airport
                {
                    Identifier = fields[4].Trim(),
                    Name = fields[12].Trim(),
                    Type = fields[2].Trim(),
                    Lat = lat,
                    Lon = lon
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MapDataService: error searching airport — {ex.Message}");
            }
        }
        return null;
    }

    public Navaid? FindNavaid(string identifier)
    {
        foreach (var fields in ReadCsv("NAV_BASE.csv"))
        {
            try
            {
                if (fields.Length < 32) continue;
                if (!fields[1].Trim().Equals(identifier,
                    StringComparison.OrdinalIgnoreCase)) continue;

                if (!double.TryParse(fields[26], out double lat)) continue;
                if (!double.TryParse(fields[31], out double lon)) continue;

                return new Navaid
                {
                    Identifier = fields[1].Trim(),
                    Name = fields[7].Trim(),
                    Type = fields[2].Trim(),
                    Lat = lat,
                    Lon = lon
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MapDataService: error searching navaid — {ex.Message}");
            }
        }
        return null;
    }

    public Waypoint? FindWaypoint(string identifier)
    {
        foreach (var fields in ReadCsv("FIX_BASE.csv"))
        {
            try
            {
                if (fields.Length < 15) continue;
                if (!fields[1].Trim().Equals(identifier,
                    StringComparison.OrdinalIgnoreCase)) continue;

                if (!double.TryParse(fields[9], out double lat)) continue;
                if (!double.TryParse(fields[14], out double lon)) continue;

                return new Waypoint
                {
                    Identifier = fields[1].Trim(),
                    Lat = lat,
                    Lon = lon
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MapDataService: error searching waypoint — {ex.Message}");
            }
        }
        return null;
    }
    public List<TraconBoundary> LoadTraconBoundaries()
    {
        var result = new List<TraconBoundary>();

        using var reader = OpenResource("tracon-boundaries.json");
        if (reader == null) return result;

        try
        {
            var json = reader.ReadToEnd();
            var doc = JsonDocument.Parse(json);
            var features = doc.RootElement.GetProperty("features");

            foreach (var feature in features.EnumerateArray())
            {
                try
                {
                    var properties = feature.GetProperty("properties");
                    var geometry = feature.GetProperty("geometry");
                    var type = geometry.GetProperty("type").GetString();
                    var coordinates = geometry.GetProperty("coordinates");

                    var tracon = new TraconBoundary
                    {
                        Identifier = properties
                            .TryGetProperty("identifier", out var idProp)
                            ? idProp.GetString() ?? string.Empty
                            : string.Empty,
                        Name = properties
                            .TryGetProperty("name", out var nameProp)
                            ? nameProp.GetString() ?? string.Empty
                            : string.Empty
                    };

                    if (type == "Polygon")
                    {
                        var ring = ParseRing(coordinates[0])
                            .Select(p => new LatLon { Lat = p.Lat, Lon = p.Lon })
                            .ToList();
                        if (ring.Count > 0)
                            tracon.Rings.Add(ring);
                    }
                    else if (type == "MultiPolygon")
                    {
                        foreach (var polygon in coordinates.EnumerateArray())
                        {
                            foreach (var ring in polygon.EnumerateArray())
                            {
                                var points = ParseRing(ring)
                                    .Select(p => new LatLon { Lat = p.Lat, Lon = p.Lon })
                                    .ToList();
                                if (points.Count > 0)
                                    tracon.Rings.Add(points);
                            }
                        }
                    }

                    if (tracon.Rings.Count > 0)
                        result.Add(tracon);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"MapDataService: skipping TRACON feature — {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"MapDataService: failed to load TRACON boundaries — {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {result.Count} TRACON boundaries");
        return result;
    }
}