using System.Collections.Generic;
using System.Linq;

namespace vTFMS.Models;

public class ArtccSector
{
    public string Artcc { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int BaseAlt { get; set; }
    public int MaxAlt { get; set; }
    public List<List<LatLon>> Rings { get; set; } = new();

    // Bounding box for fast pre-check
    public double MinLat { get; private set; }
    public double MaxLat { get; private set; }
    public double MinLon { get; private set; }
    public double MaxLonVal { get; private set; }
    public bool BoundsComputed { get; private set; }

    public string Label => $"{Artcc}-{Sector}";
    public string AltLabel =>
        $"{BaseAlt / 100}-{(MaxAlt >= 60000 ? "UNL" : (MaxAlt / 100).ToString())}";

    public void ComputeBounds()
    {
        var allPoints = Rings.SelectMany(r => r).ToList();
        if (allPoints.Count == 0) return;

        MinLat = allPoints.Min(p => p.Lat);
        MaxLat = allPoints.Max(p => p.Lat);
        MinLon = allPoints.Min(p => p.Lon);
        MaxLonVal = allPoints.Max(p => p.Lon);
        BoundsComputed = true;
    }

    public bool IsInBoundingBox(double lat, double lon)
    {
        if (!BoundsComputed) return true; // fail open if not computed
        return lat >= MinLat && lat <= MaxLat &&
               lon >= MinLon && lon <= MaxLonVal;
    }

    public bool ContainsAltitude(int altitudeFeet)
    {
        return altitudeFeet >= BaseAlt && altitudeFeet <= MaxAlt;
    }
}