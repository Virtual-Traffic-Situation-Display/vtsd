using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using vTFMS.Models;

namespace vTFMS.Services;

public partial class MapDataService
{
    // =========================================================================
    // GeoJSON parsing helpers
    // =========================================================================

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

    private static List<LatLon> ParseLineStringCoords(JsonElement coords)
    {
        var points = new List<LatLon>();
        foreach (var coord in coords.EnumerateArray())
        {
            var arr = coord.EnumerateArray().ToList();
            if (arr.Count < 2) continue;
            double lon = arr[0].GetDouble();
            double lat = arr[1].GetDouble();
            points.Add(new LatLon(lat, lon));
        }
        return points;
    }

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

    // =========================================================================
    // State and country boundaries
    // =========================================================================

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

    // =========================================================================
    // TRACON boundaries
    // =========================================================================

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

    public List<TraconBoundary> FindTracons(string identifier)
    {
        // Reuse the already-loaded TRACON data from TsdViewModel
        // rather than reloading from disk each time
        var allTracons = LoadTraconBoundaries();
        return allTracons
            .Where(t => t.Identifier.Equals(
                identifier, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // =========================================================================
    // ARTCC boundaries (from ARB_SEG.csv)
    // =========================================================================

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
            $"MapDataService: loaded {result.Count} high altitude ARTCC boundaries");

        foreach (var b in result)
            b.ComputeBounds();

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

    // =========================================================================
    // Polygon / spatial math
    // =========================================================================

    public bool IsPointInArtcc(double lat, double lon,
                               ArtccBoundary artcc)
    {
        if (!artcc.IsInBoundingBox(lat, lon))
            return false;
        return IsPointInPolygon(lat, lon, artcc.Points);
    }

    public bool IsPointInPolygon(
        double lat, double lon,
        List<LatLon> polygon)
    {
        int n = polygon.Count;
        bool inside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = polygon[i].Lon;
            double yi = polygon[i].Lat;
            double xj = polygon[j].Lon;
            double yj = polygon[j].Lat;

            bool intersect =
                ((yi > lat) != (yj > lat)) &&
                (lon < (xj - xi) * (lat - yi) /
                    (yj - yi) + xi);

            if (intersect) inside = !inside;
        }

        return inside;
    }
}