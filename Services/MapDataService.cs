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

    // Airways — loaded lazily on first request
    private Dictionary<string, Airway>? _airways = null;
    private readonly object _airwayLock = new();

    // Sectors — loaded lazily on first request
    // Key: "ARTCC-sector" e.g. "ZID-02"
    // Value: list of ArtccSector (one per unique polygon/altitude range)
    private Dictionary<string, List<ArtccSector>>? _sectors = null;
    private List<ArtccSector>? _allSectors = null;
    private readonly object _sectorLock = new();

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

    // -------------------------------------------------------------------------
    // Sectors — lazy loaded from disk on first request
    // -------------------------------------------------------------------------

    private (Dictionary<string, List<ArtccSector>> map,
             List<ArtccSector> all) EnsureSectorsLoaded()
    {
        if (_sectors != null && _allSectors != null)
            return (_sectors, _allSectors);

        lock (_sectorLock)
        {
            if (_sectors != null && _allSectors != null)
                return (_sectors, _allSectors);

            var map = new Dictionary<string, List<ArtccSector>>(
                StringComparer.OrdinalIgnoreCase);
            var all = new List<ArtccSector>();

            var filePath = Path.Combine(
                AppContext.BaseDirectory, "Data", "sectors.json");

            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine(
                    "MapDataService: sectors.json not found");
                _sectors = map;
                _allSectors = all;
                return (_sectors, _allSectors);
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var doc = JsonDocument.Parse(json);
                var features = doc.RootElement.GetProperty("features");

                foreach (var feature in features.EnumerateArray())
                {
                    try
                    {
                        var props = feature.GetProperty("properties");

                        var artcc = props.GetProperty("artcc")
                            .GetString() ?? string.Empty;
                        var sector = props.GetProperty("sector")
                            .GetString() ?? string.Empty;
                        var tier = props.GetProperty("tier")
                            .GetString() ?? string.Empty;
                        var baseAlt = props.GetProperty("base_alt")
                            .GetInt32();
                        var maxAlt = props.GetProperty("max_alt")
                            .GetInt32();

                        if (string.IsNullOrWhiteSpace(artcc) ||
                            string.IsNullOrWhiteSpace(sector)) continue;

                        var geometry = feature.GetProperty("geometry");
                        var geomType = geometry.GetProperty("type")
                            .GetString();
                        var coordinates = geometry.GetProperty("coordinates");

                        var rings = new List<List<LatLon>>();

                        if (geomType == "Polygon")
                        {
                            rings.Add(ParseLineStringCoords(coordinates[0]));
                        }
                        else if (geomType == "MultiPolygon")
                        {
                            foreach (var poly in coordinates.EnumerateArray())
                                rings.Add(ParseLineStringCoords(poly[0]));
                        }
                        else if (geomType == "LineString")
                        {
                            rings.Add(ParseLineStringCoords(coordinates));
                        }

                        if (rings.Count == 0 || rings.All(r => r.Count < 3))
                            continue;

                        var artccSector = new ArtccSector
                        {
                            Artcc = artcc,
                            Sector = sector,
                            Tier = tier,
                            BaseAlt = baseAlt,
                            MaxAlt = maxAlt,
                            Rings = rings
                        };

                        artccSector.ComputeBounds();

                        var key = $"{artcc}-{sector}".ToUpperInvariant();
                        if (!map.TryGetValue(key, out var list))
                        {
                            list = new List<ArtccSector>();
                            map[key] = list;
                        }
                        list.Add(artccSector);
                        all.Add(artccSector);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"MapDataService: skipping sector — {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MapDataService: failed to load sectors.json — {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine(
                $"MapDataService: loaded {all.Count} sector polygons " +
                $"across {map.Count} sector identifiers");

            _sectors = map;
            _allSectors = all;
            return (_sectors, _allSectors);
        }
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

    public List<ArtccSector> GetSectors(string identifier)
    {
        var (map, _) = EnsureSectorsLoaded();
        map.TryGetValue(identifier.ToUpperInvariant(), out var list);
        return list ?? new List<ArtccSector>();
    }

    public List<string> GetArtccsWithSectors()
    {
        var (_, all) = EnsureSectorsLoaded();
        return all.Select(s => s.Artcc)
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .OrderBy(s => s)
                  .ToList();
    }

    public List<ArtccSector> GetSectorsForArtcc(string artcc)
    {
        var (_, all) = EnsureSectorsLoaded();
        return all
            .Where(s => s.Artcc.Equals(artcc,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Sector)
            .ToList();
    }

    public ArtccSector? FindSectorForPosition(
        double lat, double lon, int altitudeFeet)
    {
        var (_, all) = EnsureSectorsLoaded();

        // Pass 1 — altitude-filtered check with bounding box
        foreach (var sector in all)
        {
            if (!sector.ContainsAltitude(altitudeFeet)) continue;
            if (!sector.IsInBoundingBox(lat, lon)) continue;

            foreach (var ring in sector.Rings)
            {
                if (IsPointInPolygon(lat, lon, ring))
                    return sector;
            }
        }

        // Pass 2 — fallback: ignore altitude, check all sectors
        foreach (var sector in all)
        {
            if (!sector.IsInBoundingBox(lat, lon)) continue;

            foreach (var ring in sector.Rings)
            {
                if (IsPointInPolygon(lat, lon, ring))
                    return sector;
            }
        }

        return null;
    }

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

    // -------------------------------------------------------------------------
    // Airways — lazy loaded from disk on first request
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Route resolution
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // GeoJSON / CSV helpers
    // -------------------------------------------------------------------------

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
}