using System.Collections.Generic;

namespace vTFMS.Models;

public class ArtccThreshold
{
    public string Identifier { get; set; } = string.Empty;
    public int YellowAt { get; set; } = 12;
    public int RedAt { get; set; } = 20;
}

public class SectorCombineRule
{
    public string Artcc { get; set; } = string.Empty;
    public string Parent { get; set; } = string.Empty;
    public List<string> Children { get; set; } = new();
}

public class TraconMonitorConfig
{
    public string Identifier { get; set; } = string.Empty;
    public int AltitudeCeiling { get; set; } = 99999;
}

public class NasMonitorSettings
{
    public int HorizonMinutes { get; set; } = 60;
    public List<ArtccThreshold> Thresholds { get; set; } = new();
    public List<SectorCombineRule> CombineRules { get; set; } = new();
    public List<TraconMonitorConfig> MonitoredTracons { get; set; } = new();
}
