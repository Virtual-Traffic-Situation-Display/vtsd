namespace vTFMS.Models;

public class LatLon
{
    public double Lat { get; set; }
    public double Lon { get; set; }

    public LatLon() { }

    public LatLon(double lat, double lon)
    {
        Lat = lat;
        Lon = lon;
    }
}