using System.Collections.Generic;

namespace vTFMS.Models;

public class ArtccBoundary
{
    public string Identifier { get; set; } = string.Empty;
    public List<LatLon> Points { get; set; } = new();

    // Bounding box for fast pre-check
    public double MinLat { get; set; } = double.MaxValue;
    public double MaxLat { get; set; } = double.MinValue;
    public double MinLon { get; set; } = double.MaxValue;
    public double MaxLon { get; set; } = double.MinValue;

    public void ComputeBounds()
    {
        foreach (var p in Points)
        {
            if (p.Lat < MinLat) MinLat = p.Lat;
            if (p.Lat > MaxLat) MaxLat = p.Lat;
            if (p.Lon < MinLon) MinLon = p.Lon;
            if (p.Lon > MaxLon) MaxLon = p.Lon;
        }
    }

    public bool IsInBoundingBox(double lat, double lon)
    {
        return lat >= MinLat && lat <= MaxLat &&
               lon >= MinLon && lon <= MaxLon;
    }
}