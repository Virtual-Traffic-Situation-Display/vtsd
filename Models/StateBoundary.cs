using System.Collections.Generic;

namespace vTFMS.Models;

public class StateBoundary
{
    public string Name { get; set; } = string.Empty;
    public List<LatLon> Points { get; set; } = new();
}