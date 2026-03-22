using System.Collections.Generic;

namespace vTFMS.Models;

public class Airway
{
    public string Identifier { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // J, V, Q, T, A, etc.
    public List<string> WaypointNames { get; set; } = new();
    public List<LatLon?> ResolvedPoints { get; set; } = new();
    public bool IsResolved { get; set; } = false;
}