using System.Collections.Generic;

namespace vTFMS.Models;

public class VatsimPilot
{
    public string Callsign { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int Altitude { get; set; }
    public int GroundSpeed { get; set; }
    public int Heading { get; set; }
    public string AircraftType { get; set; } = string.Empty;
    public string Departure { get; set; } = string.Empty;
    public string Arrival { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string MatchedFilterColor { get; set; } = "#FFFFFF";
    public bool MatchedDrawRoute { get; set; } = false;
    public bool MatchedShowRoute { get; set; } = false;
    public List<LatLon> ParsedRoute { get; set; } = new();
}