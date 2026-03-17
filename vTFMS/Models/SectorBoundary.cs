using System.Collections.Generic;

namespace vTFMS.Models;

public class SectorBoundary
{
    public string Name { get; set; } = string.Empty;
    public List<(double Lat, double Lon)> Points { get; set; } = new();
}