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

    private readonly Dictionary<string, Airport> _airports = new();
    private readonly Dictionary<string, Navaid> _navaids = new();
    private readonly Dictionary<string, Waypoint> _waypoints = new();

    public MapDataService()
    {
        LoadAirports();
        LoadNavaids();
        LoadWaypoints();
    }

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

    private static List<LatLon> ParseRing(JsonElement ring)
    {
        var points = new List<LatLon>();
        foreach (var coord in ring.EnumerateArray())
        {
            var lon = coord[0].GetDouble();
            var lat = coord[1].GetDouble();
            points.Add(new LatLon { Lat = lat, Lon = lon });
        }
        return points;
    }

    /// <summary>
    /// Shared GeoJSON feature parser. Reads all features from a JSON document,
    /// applies a filter, extracts a name, and returns StateBoundary objects
    /// for each Polygon or MultiPolygon ring that passes the filter.
    /// </summary>
    private static List<StateBoundary> ParseGeoJsonStateBoundaries(
        string resourceFileName,
        Func<JsonElement, bool> featureFilter,
        Func<JsonElement, string> nameSelector,
        string errorLabel)
    {
        var result = new List<StateBoundary>();

        using var reader = OpenResource(resourceFileName);
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
                    if (!featureFilter(feature)) continue;

                    var name = nameSelector(feature);
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
                        $"MapDataService: skipping {errorLabel} feature — {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"MapDataService: failed to load {errorLabel} — {ex.Message}");
        }

        return result;
    }

    public List<StateBoundary> LoadStateBoundaries()
    {
        var result = ParseGeoJsonStateBoundaries(
            "na-provinces.json",
            feature =>
            {
                var props = feature.GetProperty("properties");
                return props.TryGetProperty("admin", out var admin) &&
                       admin.GetString() == "United States of America";
            },
            feature =>
            {
                var props = feature.GetProperty("properties");
                return props.TryGetProperty("name", out var name)
                    ? name.GetString() ?? string.Empty
                    : string.Empty;
            },
            "state");

        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {result.Count} state boundaries");
        return result;
    }

    public List<StateBoundary> LoadCountryBoundaries()
    {
        // Canadian provinces
        var result = ParseGeoJsonStateBoundaries(
            "na-provinces.json",
            feature =>
            {
                var props = feature.GetProperty("properties");
                return props.TryGetProperty("admin", out var admin) &&
                       admin.GetString() == "Canada";
            },
            feature =>
            {
                var props = feature.GetProperty("properties");
                return props.TryGetProperty("name", out var name)
                    ? name.GetString() ?? string.Empty
                    : string.Empty;
            },
            "Canada province");

        // Caribbean and Mexico
        var caribbeanAndMexico = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Mexico", "Cuba", "Jamaica", "Haiti", "Dominican Republic",
            "Puerto Rico", "Bahamas", "Trinidad and Tobago", "Barbados",
            "Saint Lucia", "Saint Vincent and the Grenadines", "Grenada",
            "Antigua and Barbuda", "Dominica", "Saint Kitts and Nevis",
            "Aruba", "Curacao", "Sint Maarten", "Turks and Caicos Islands",
            "Cayman Islands", "British Virgin Islands", "U.S. Virgin Islands",
            "Anguilla", "Montserrat", "Guadeloupe", "Martinique",
            "Saint Barthelemy", "Saint Martin", "Belize", "Guatemala",
            "Honduras", "El Salvador", "Nicaragua", "Costa Rica", "Panama"
        };

        var caribbean = ParseGeoJsonStateBoundaries(
            "na-countries.json",
            feature =>
            {
                var props = feature.GetProperty("properties");
                string name = string.Empty;
                if (props.TryGetProperty("NAME", out var n) ||
                    props.TryGetProperty("name", out n))
                    name = n.GetString() ?? string.Empty;
                return caribbeanAndMexico.Contains(name);
            },
            feature =>
            {
                var props = feature.GetProperty("properties");
                if (props.TryGetProperty("NAME", out var n) ||
                    props.TryGetProperty("name", out n))
                    return n.GetString() ?? string.Empty;
                return string.Empty;
            },
            "Caribbean/Mexico");

        result.AddRange(caribbean);

        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {result.Count} country boundaries");
        return result;
    }

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

    public List<LatLon> ResolveRoute(string departure,
        string route, string arrival)
    {
        var result = new List<LatLon>();

        var dep = FindAirport(departure);
        if (dep != null)
            result.Add(new LatLon(dep.Lat, dep.Lon));

        var tokens = route
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !IsAirway(t))
            .ToList();

        foreach (var token in tokens)
        {
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

    private static bool IsAirway(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return true;
        if (token == "DCT") return true;
        return token.Length >= 2 &&
               char.IsLetter(token[0]) &&
               char.IsDigit(token[1]);
    }

    private static LatLon? TryParseCoordWaypoint(string token)
    {
        try
        {
            int nIdx = token.IndexOfAny(new[] { 'N', 'S' });
            int ewIdx = token.IndexOfAny(new[] { 'E', 'W' });

            if (nIdx <= 0 || ewIdx <= nIdx) return null;

            double lat = double.Parse(token[..nIdx]);
            double lon = double.Parse(token[(nIdx + 1)..ewIdx]);

            if (token[nIdx] == 'S') lat = -lat;
            if (token[ewIdx] == 'W') lon = -lon;

            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                return null;

            return new LatLon(lat, lon);
        }
        catch
        {
            return null;
        }
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
                        var ring = ParseRing(coordinates[0]);
                        if (ring.Count > 0)
                            tracon.Rings.Add(ring);
                    }
                    else if (type == "MultiPolygon")
                    {
                        foreach (var polygon in coordinates.EnumerateArray())
                        {
                            foreach (var ring in polygon.EnumerateArray())
                            {
                                var points = ParseRing(ring);
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

    public List<ArtccBoundary> LoadArtccBoundaries()
    {
        var result = new List<ArtccBoundary>();
        var filePath = Path.Combine(
            AppContext.BaseDirectory, "Data", "ARB_SEG.csv");

        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine(
                "MapDataService: ARB_SEG.csv not found");
            return result;
        }

        var boundaries = new Dictionary<string, ArtccBoundary>();

        foreach (var line in File.ReadLines(filePath).Skip(1))
        {
            var fields = line.Split(',')
                .Select(f => f.Trim().Trim('"'))
                .ToArray();

            if (fields.Length < 17) continue;

            var altType = fields[4].Trim();
            if (!altType.Equals("HIGH",
                StringComparison.OrdinalIgnoreCase))
                continue;

            var id = fields[2].Trim();

            if (!double.TryParse(fields[11],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double lat))
                continue;

            if (!double.TryParse(fields[16],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double lon))
                continue;

            if (lon > 0)
                lon = -(180 + (180 - lon));

            if (!boundaries.TryGetValue(id, out var boundary))
            {
                boundary = new ArtccBoundary { Identifier = id };
                boundaries[id] = boundary;
                result.Add(boundary);
            }

            boundary.Points.Add(new LatLon(lat, lon));

            var description = fields.Length > 17
                ? fields[17].Trim() : string.Empty;
            if (description.Contains("TO POINT OF BEGINNING",
                StringComparison.OrdinalIgnoreCase) &&
                boundary.Points.Count > 1)
            {
                boundary.Points.Add(new LatLon(double.NaN, double.NaN));
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"MapDataService: loaded {result.Count} " +
            $"high altitude ARTCC boundaries");
        return result;
    }

    private static bool TryParseDms(string dms, out double degrees)
    {
        degrees = 0;
        if (string.IsNullOrWhiteSpace(dms)) return false;

        try
        {
            char hemi = dms[^1];
            string digits = dms[..^1];

            bool isLon = hemi == 'E' || hemi == 'W';
            int degLen = isLon ? 3 : 2;

            int deg = int.Parse(digits[..degLen]);
            int min = int.Parse(digits[degLen..(degLen + 2)]);
            double sec = int.Parse(digits[(degLen + 2)..]) / 100.0;

            degrees = deg + min / 60.0 + sec / 3600.0;

            if (hemi == 'S' || hemi == 'W')
                degrees = -degrees;

            return true;
        }
        catch
        {
            return false;
        }
    }
}