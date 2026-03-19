using System.Collections.Generic;

namespace vTFMS.Models;

public class ArtccThreshold
{
    public string Identifier { get; set; } = string.Empty;
    public int YellowAt { get; set; } = 12;
    public int RedAt { get; set; } = 20;
}

public class NasMonitorSettings
{
    public int HorizonMinutes { get; set; } = 60;
    public List<ArtccThreshold> Thresholds { get; set; } = new();
}