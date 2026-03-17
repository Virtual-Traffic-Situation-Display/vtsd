using System.Collections.Generic;
using vTFMS.Models;

namespace vTFMS.Models;

public class FlightFilterProfile
{
    public string Name { get; set; } = "Default";
    public List<FlightFilter> Filters { get; set; } = new();
    public double CenterLat { get; set; } = 39.5;
    public double CenterLon { get; set; } = -98.35;
    public double ZoomLevel { get; set; } = 1.0;
    public bool ShowStateBoundaries { get; set; } = true;
    public bool ShowCountryBoundaries { get; set; } = true;
    public List<MapItem> ActiveMapItems { get; set; } = new();
    public List<List<LatLon>> Rings { get; set; } = new();
}

public class LatLon
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}