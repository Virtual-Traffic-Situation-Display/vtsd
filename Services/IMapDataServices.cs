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
}