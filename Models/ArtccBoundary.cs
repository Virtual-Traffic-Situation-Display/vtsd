using System.Collections.Generic;

namespace vTFMS.Models;

public class ArtccBoundary
{
    public string Identifier { get; set; } = string.Empty;
    public List<LatLon> Points { get; set; } = new();
}