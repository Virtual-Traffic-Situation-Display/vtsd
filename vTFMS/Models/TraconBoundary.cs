using System.Collections.Generic;

namespace vTFMS.Models;

public class TraconBoundary
{
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<List<LatLon>> Rings { get; set; } = new();
}