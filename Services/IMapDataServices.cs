using System.Collections.Generic;
using vTFMS.Models;

namespace vTFMS.Services;

public interface IMapDataService
{
    List<StateBoundary> LoadStateBoundaries();
    List<StateBoundary> LoadCountryBoundaries();
    Airport? FindAirport(string identifier);
    Navaid? FindNavaid(string identifier);
    Waypoint? FindWaypoint(string identifier);
    List<TraconBoundary> LoadTraconBoundaries();
    List<ArtccBoundary> LoadArtccBoundaries();
    List<LatLon> ResolveRoute(string departure, string route,
                           string arrival);

    bool IsPointInPolygon(double lat, double lon,
                          List<LatLon> polygon);

    bool IsPointInArtcc(double lat, double lon,
                        ArtccBoundary artcc);
}