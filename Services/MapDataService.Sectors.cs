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
    // Sectors — lazy loaded from disk on first request
    // =========================================================================

    // Key: "ARTCC-sector" e.g. "ZID-02"
    // Value: list of ArtccSector (one per unique polygon/altitude range)
    private Dictionary<string, List<ArtccSector>>? _sectors = null;
    private List<ArtccSector>? _allSectors = null;
    private readonly object _sectorLock = new();

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

    // =========================================================================
    // Sector queries
    // =========================================================================

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
}