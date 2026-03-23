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
    List<TraconBoundary> FindTracons(string identifier);
    List<ArtccBoundary> LoadArtccBoundaries();
    List<LatLon> ResolveRoute(string departure, string route,
                              string arrival);
    bool IsPointInPolygon(double lat, double lon,
                          List<LatLon> polygon);
    bool IsPointInArtcc(double lat, double lon,
                        ArtccBoundary artcc);
    Airway? GetAirway(string identifier);
    LatLon? ResolveWaypoint(string identifier);

    // Sectors
    List<ArtccSector> GetSectors(string identifier); // e.g. "ZID-02"
    List<string> GetArtccsWithSectors();
    List<ArtccSector> GetSectorsForArtcc(string artcc);
    ArtccSector? FindSectorForPosition(double lat, double lon,
                                       int altitudeFeet);
    int EstimateAltitude(VatsimPilot pilot, LatLon projectedPos);
}
