using System;
using System.Collections.Generic;
using vTFMS.Models;

namespace vTFMS.Services;

public static class RouteProjector
{
    // Earth radius in nautical miles
    private const double EarthRadiusNm = 3440.065;

    public static LatLon? ProjectPosition(
        VatsimPilot pilot, int minutesAhead)
    {
        if (pilot.ParsedRoute == null ||
            pilot.ParsedRoute.Count < 2)
            return null;

        if (pilot.AverageSpeed <= 0)
            return null;

        // Distance to travel in nautical miles
        double distanceNm =
            pilot.AverageSpeed * minutesAhead / 60.0;

        // Find the segment the aircraft is currently on
        int segmentIndex = FindCurrentSegment(
            pilot.Lat, pilot.Lon, pilot.ParsedRoute);

        // Walk forward along route consuming distance
        double remaining = distanceNm;
        int i = segmentIndex;

        // First consume distance from current position
        // to next waypoint
        double distToNext = DistanceNm(
            pilot.Lat, pilot.Lon,
            pilot.ParsedRoute[i + 1].Lat,
            pilot.ParsedRoute[i + 1].Lon);

        while (remaining > distToNext && i < pilot.ParsedRoute.Count - 2)
        {
            remaining -= distToNext;
            i++;
            distToNext = DistanceNm(
                pilot.ParsedRoute[i].Lat,
                pilot.ParsedRoute[i].Lon,
                pilot.ParsedRoute[i + 1].Lat,
                pilot.ParsedRoute[i + 1].Lon);
        }

        // Aircraft runs out of route — return last waypoint
        if (i >= pilot.ParsedRoute.Count - 1)
            return pilot.ParsedRoute[^1];

        // Interpolate between waypoint i and i+1
        double fraction = distToNext > 0
            ? remaining / distToNext : 0;

        double lat = pilot.ParsedRoute[i].Lat +
            fraction * (pilot.ParsedRoute[i + 1].Lat -
                pilot.ParsedRoute[i].Lat);
        double lon = pilot.ParsedRoute[i].Lon +
            fraction * (pilot.ParsedRoute[i + 1].Lon -
                pilot.ParsedRoute[i].Lon);

        return new LatLon(lat, lon);
    }

    private static int FindCurrentSegment(
        double lat, double lon,
        List<LatLon> route)
    {
        double minDist = double.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < route.Count - 1; i++)
        {
            var closest = ClosestPointOnSegment(
                lat, lon,
                route[i].Lat, route[i].Lon,
                route[i + 1].Lat, route[i + 1].Lon);

            double dist = DistanceNm(
                lat, lon, closest.Lat, closest.Lon);

            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    // Planar approximation — treats lat/lon as Cartesian axes.
    // At higher latitudes longitude degrees are shorter than latitude
    // degrees, so the projection is slightly biased. Acceptable for
    // CONUS segment-finding where waypoints are close together.
    private static LatLon ClosestPointOnSegment(
        double pLat, double pLon,
        double aLat, double aLon,
        double bLat, double bLon)
    {
        double dx = bLon - aLon;
        double dy = bLat - aLat;
        double lenSq = dx * dx + dy * dy;

        if (lenSq == 0)
            return new LatLon(aLat, aLon);

        double t = ((pLon - aLon) * dx +
                    (pLat - aLat) * dy) / lenSq;
        t = Math.Clamp(t, 0, 1);

        return new LatLon(
            aLat + t * dy,
            aLon + t * dx);
    }

    public static double DistanceNm(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) *
            Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(
            Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusNm * c;
    }
}