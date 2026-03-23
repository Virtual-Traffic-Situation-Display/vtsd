using System.Collections.Generic;

namespace vTFMS.Models;

public class MapItem
{
    public string Identifier { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Color { get; set; } = "#FFFFFF";
    public string Label { get; set; } = string.Empty;
    public List<List<LatLon>> Rings { get; set; } = new();
    public override string ToString() => Identifier;
}