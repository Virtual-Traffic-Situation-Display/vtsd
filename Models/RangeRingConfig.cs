namespace vTFMS.Models;

public class RangeRingConfig
{
    public string Identifier { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLon { get; set; }
    public int IntervalNm { get; set; }
    public int DistanceNm { get; set; }
}