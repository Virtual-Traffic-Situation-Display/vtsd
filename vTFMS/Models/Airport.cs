namespace vTFMS.Models;

public class Airport
{
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Type { get; set; } = string.Empty;
}