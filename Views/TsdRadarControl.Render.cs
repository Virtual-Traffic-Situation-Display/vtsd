using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using vTFMS.Models;

namespace vTFMS.Views;

public partial class TsdRadarControl
{
    // =========================================================================
    // Render — main entry point
    // =========================================================================

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        // Background
        context.FillRectangle(
            _backgroundBrush, new Rect(0, 0, width, height));

        // Rebuild cached geometries if stale
        if (_geometriesDirty ||
            Math.Abs(_cachedWidth - width) > 0.5 ||
            Math.Abs(_cachedHeight - height) > 0.5)
        {
            RebuildGeometries(width, height);
        }

        // Boundaries
        if (ShowStateBoundaries && _stateBoundaryGeometry != null)
            context.DrawGeometry(
                null, _boundaryPen, _stateBoundaryGeometry);

        if (ShowCountryBoundaries && _countryBoundaryGeometry != null)
            context.DrawGeometry(
                null, _boundaryPen, _countryBoundaryGeometry);

        if (_sectorGeometry != null)
            context.DrawGeometry(null, _sectorPen, _sectorGeometry);

        // Weather radar overlay
        if (ShowWeather && _radarBitmap != null)
        {
            var topLeft = LatLonToScreen(
                RadarMaxLat, RadarMinLon, width, height);
            var bottomRight = LatLonToScreen(
                RadarMinLat, RadarMaxLon, width, height);

            var destRect = new Rect(
                topLeft.X, topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y);

            using (context.PushOpacity(RadarOpacity))
            {
                context.DrawImage(_radarBitmap,
                    new Rect(0, 0,
                        _radarBitmap.PixelSize.Width,
                        _radarBitmap.PixelSize.Height),
                    destRect);
            }
        }

        // ARTCC boundaries
        if (ShowArtcc && _artccBoundaryGeometry != null)
            context.DrawGeometry(null, _artccPen, _artccBoundaryGeometry);

        // Overlays and flights
        DrawRangeRings(context, width, height);
        DrawRoutes(context, width, height);
        DrawActiveMapItems(context, width, height);
        DrawAircraft(context, width, height);
        DrawLeadLines(context, width, height);
    }

    // =========================================================================
    // Draw — Aircraft and data blocks
    // =========================================================================

    private void DrawAircraft(DrawingContext context,
                               double width, double height)
    {
        var typeface = _dataBlockTypeface;
        const double size = 4.0;

        foreach (var pilot in VisiblePilots)
        {
            if (pilot.IsHidden) continue;

            var pt = LatLonToScreen(
                pilot.Lat, pilot.Lon, width, height);
            if (!IsOnScreen(pt, width, height)) continue;

            var color = pilot.FoundColor
                ?? pilot.ColorOverride
                ?? pilot.MatchedFilterColor;
            var brush = new SolidColorBrush(Color.Parse(color));

            DrawAircraftSymbol(context, pt, pilot.Heading, size, brush);

            // Show data block if global setting, persistent, found, or on hover
            if (FlightDisplaySettings.ShowDataBlocks ||
                pilot.ShowDataBlock ||
                pilot.IsFound ||
                (_hoveredCallsign != null &&
                 _hoveredCallsign == pilot.Callsign))
            {
                DrawDataBlock(context, pt, pilot,
                    _dataBlockBrush, typeface);
            }
        }
    }

    private void DrawAircraftSymbol(DrawingContext context,
        Point pt, int heading, double size, IBrush brush)
    {
        double scale = size / 6.0;
        double rad = heading * Math.PI / 180.0;

        Point Transform(double x, double y)
        {
            double rx = x * Math.Cos(rad) - y * Math.Sin(rad);
            double ry = x * Math.Sin(rad) + y * Math.Cos(rad);
            return new Point(pt.X + rx * scale, pt.Y + ry * scale);
        }

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(Transform(0, -6.5), true);
            ctx.LineTo(Transform(1, -8));
            ctx.LineTo(Transform(1, -2));
            ctx.LineTo(Transform(8, 4));
            ctx.LineTo(Transform(6, 5));
            ctx.LineTo(Transform(1, 1));
            ctx.LineTo(Transform(1, 6));
            ctx.LineTo(Transform(4, 9));
            ctx.LineTo(Transform(4, 10));
            ctx.LineTo(Transform(1, 8));
            ctx.LineTo(Transform(0, 8));
            ctx.LineTo(Transform(-1, 8));
            ctx.LineTo(Transform(-4, 10));
            ctx.LineTo(Transform(-4, 9));
            ctx.LineTo(Transform(-1, 6));
            ctx.LineTo(Transform(-1, 1));
            ctx.LineTo(Transform(-6, 5));
            ctx.LineTo(Transform(-8, 4));
            ctx.LineTo(Transform(-1, -2));
            ctx.LineTo(Transform(-1, -8));
            ctx.EndFigure(true);
        }

        context.DrawGeometry(brush, null, geo);
    }

    private void DrawDataBlock(DrawingContext context,
        Point pt, VatsimPilot pilot,
        IBrush textBrush, Typeface typeface)
    {
        string altStr = pilot.Altitude >= 18000
            ? $"F{pilot.Altitude / 100:000}"
            : $"{pilot.Altitude / 100:000}";

        var linesList = new List<string>
        {
            pilot.Callsign,
            $"{pilot.AircraftType,-4} {altStr}",
            $"{pilot.GroundSpeed}"
        };

        if (pilot.ShowOrgDest || FlightDisplaySettings.ShowOrgDest)
            linesList.Add($"{pilot.Departure} → {pilot.Arrival}");
        else
            linesList.Add(pilot.Arrival);

        if ((pilot.MatchedShowRoute || pilot.ManualShowRoute ||
             FlightDisplaySettings.ShowRouteText) &&
            !string.IsNullOrWhiteSpace(pilot.Route))
        {
            const int wrapWidth = 30;
            var route = pilot.Route;
            while (route.Length > 0)
            {
                if (route.Length <= wrapWidth)
                {
                    linesList.Add(route);
                    break;
                }

                int breakAt = route.LastIndexOf(' ', wrapWidth);
                if (breakAt <= 0) breakAt = wrapWidth;

                linesList.Add(route[..breakAt].Trim());
                route = route[breakAt..].Trim();
            }
        }

        var lines = linesList.ToArray();
        double lineHeight = 12.0;
        double bx = pt.X + 10;
        double by = pt.Y - (lines.Length * lineHeight) / 2;

        for (int i = 0; i < lines.Length; i++)
        {
            var text = new FormattedText(
                lines[i],
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface, 10, textBrush);

            context.DrawText(text, new Point(bx, by + i * lineHeight));
        }
    }

    // =========================================================================
    // Draw — Flight routes
    // =========================================================================

    private void DrawRoutes(DrawingContext context,
        double width, double height)
    {
        foreach (var pilot in VisiblePilots)
        {
            if (pilot.ForceHideRoute) continue;
            if (!pilot.MatchedDrawRoute && !pilot.ManualDrawRoute &&
                !FlightDisplaySettings.DrawRoutes) continue;
            if (pilot.IsHidden) continue;
            if (pilot.ParsedRoute is null ||
                pilot.ParsedRoute.Count < 2) continue;

            var brush = new SolidColorBrush(
                Color.Parse(pilot.ColorOverride ?? pilot.MatchedFilterColor));
            var pen = new Pen(brush, 1.0);

            int nextWaypointIndex = FindNextWaypoint(
                pilot.Lat, pilot.Lon, pilot.ParsedRoute);

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                var aircraftPt = LatLonToScreen(
                    pilot.Lat, pilot.Lon, width, height);
                ctx.BeginFigure(aircraftPt, false);

                for (int i = nextWaypointIndex;
                    i < pilot.ParsedRoute.Count; i++)
                {
                    if (pilot.ParsedRoute[i] is null) continue;
                    ctx.LineTo(LatLonToScreen(
                        pilot.ParsedRoute[i]!.Lat,
                        pilot.ParsedRoute[i]!.Lon,
                        width, height));
                }
                ctx.EndFigure(false);
            }

            context.DrawGeometry(null, pen, geo);
        }
    }

    private static int FindNextWaypoint(
        double pilotLat, double pilotLon,
        List<LatLon> route)
    {
        double minDist = double.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < route.Count; i++)
        {
            double dLat = route[i].Lat - pilotLat;
            double dLon = route[i].Lon - pilotLon;
            double dist = dLat * dLat + dLon * dLon;

            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }

        return Math.Min(closestIndex + 1, route.Count - 1);
    }

    // =========================================================================
    // Draw — Range rings
    // =========================================================================

    private void DrawRangeRings(DrawingContext context,
        double width, double height)
    {
        if (RangeRings.Count == 0) return;

        var pen = new Pen(new SolidColorBrush(
            Color.Parse("#888888")), 0.8);
        var labelBrush = new SolidColorBrush(
            Color.Parse("#888888"));

        foreach (var config in RangeRings)
        {
            int ringCount = config.DistanceNm / config.IntervalNm;

            for (int r = 1; r <= ringCount; r++)
            {
                double radiusNm = r * config.IntervalNm;
                double radiusDegLat = radiusNm / 60.0;
                double radiusDegLon = radiusDegLat /
                    Math.Cos(config.CenterLat * Math.PI / 180.0);

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    const int segments = 72;
                    for (int i = 0; i <= segments; i++)
                    {
                        double angle = 2.0 * Math.PI * i / segments;
                        double lat = config.CenterLat +
                            radiusDegLat * Math.Sin(angle);
                        double lon = config.CenterLon +
                            radiusDegLon * Math.Cos(angle);

                        var pt = LatLonToScreen(lat, lon, width, height);

                        if (i == 0)
                            ctx.BeginFigure(pt, false);
                        else
                            ctx.LineTo(pt);
                    }
                    ctx.EndFigure(true);
                }

                context.DrawGeometry(null, pen, geo);

                // Distance label at top of ring
                double labelLat = config.CenterLat + radiusDegLat;
                var labelPt = LatLonToScreen(
                    labelLat, config.CenterLon, width, height);

                var ringLabel = new FormattedText(
                    $"{radiusNm}nm",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _mapLabelTypeface, 12, labelBrush);

                context.DrawText(ringLabel, new Point(
                    labelPt.X - ringLabel.Width / 2,
                    labelPt.Y - ringLabel.Height - 2));
            }

            // Center dot and identifier
            var centerPt = LatLonToScreen(
                config.CenterLat, config.CenterLon, width, height);

            context.FillRectangle(labelBrush,
                new Rect(centerPt.X - 2, centerPt.Y - 2, 4, 4));

            var label = new FormattedText(
                config.Identifier,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _mapLabelTypeface, 9, labelBrush);

            context.DrawText(label, new Point(
                centerPt.X + 4, centerPt.Y - label.Height / 2));
        }
    }

    // =========================================================================
    // Draw — Active map items (airports, VORs, NDBs, fixes, TRACONs, etc.)
    // =========================================================================

    private void DrawActiveMapItems(DrawingContext context,
                                     double width, double height)
    {
        const double half = 2.5;
        const double triSize = 5.0;

        foreach (var item in ActiveMapItems)
        {
            var pt = LatLonToScreen(item.Lat, item.Lon, width, height);
            if (item.Type != "Airway" && item.Type != "Sector" &&
                !IsOnScreen(pt, width, height)) continue;

            switch (item.Type)
            {
                case "Airport":
                    context.FillRectangle(_airportBrush,
                        new Rect(pt.X - half, pt.Y - half,
                            half * 2, half * 2));

                    if (item.ShowLabel)
                    {
                        var airportLabel = new FormattedText(
                            item.Identifier.Length > 3
                                ? item.Identifier[..3]
                                : item.Identifier,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            _mapLabelTypeface, 9, _airportBrush);

                        context.DrawText(airportLabel,
                            new Point(pt.X + half + 2,
                                pt.Y - airportLabel.Height / 2));
                    }
                    break;

                case string t when t.Contains("VOR"):
                    context.DrawEllipse(null, _vorPen, pt, 2.5, 4.0);

                    if (item.ShowLabel)
                    {
                        var vorLabel = new FormattedText(
                            item.Identifier.Length > 3
                                ? item.Identifier[..3]
                                : item.Identifier,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            _mapLabelTypeface, 9, _vorBrush);

                        context.DrawText(vorLabel,
                            new Point(pt.X + 6,
                                pt.Y - vorLabel.Height / 2));
                    }
                    break;

                case string t when t.Contains("NDB"):
                    context.DrawEllipse(null, _ndbPen, pt, 2.5, 4.0);

                    if (item.ShowLabel)
                    {
                        var ndbLabel = new FormattedText(
                            item.Identifier.Length > 3
                                ? item.Identifier[..3]
                                : item.Identifier,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            _mapLabelTypeface, 9, _ndbBrush);

                        context.DrawText(ndbLabel,
                            new Point(pt.X + 6,
                                pt.Y - ndbLabel.Height / 2));
                    }
                    break;

                case "Fix":
                    var top = new Point(pt.X, pt.Y - triSize);
                    var bottomLeft = new Point(
                        pt.X - triSize * 0.866, pt.Y + triSize * 0.5);
                    var bottomRight = new Point(
                        pt.X + triSize * 0.866, pt.Y + triSize * 0.5);

                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        ctx.BeginFigure(top, false);
                        ctx.LineTo(bottomLeft);
                        ctx.LineTo(bottomRight);
                        ctx.EndFigure(true);
                    }
                    context.DrawGeometry(null, _fixPen, geo);

                    if (item.ShowLabel)
                    {
                        var fixLabel = new FormattedText(
                            item.Identifier.Length > 5
                                ? item.Identifier[..5]
                                : item.Identifier,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            _mapLabelTypeface, 9, _fixBrush);

                        context.DrawText(fixLabel,
                            new Point(pt.X + triSize + 2,
                                pt.Y - fixLabel.Height / 2));
                    }
                    break;

                case "TRACON":
                    DrawTraconBoundary(context, item, width, height);
                    break;

                case "Sector":
                    // Draw sector boundary rings
                    foreach (var ring in item.Rings)
                    {
                        if (ring.Count < 2) continue;

                        var sectorGeo = new StreamGeometry();
                        using (var ctx = sectorGeo.Open())
                        {
                            var first = LatLonToScreen(
                                ring[0].Lat, ring[0].Lon, width, height);
                            ctx.BeginFigure(first, false);
                            for (int i = 1; i < ring.Count; i++)
                            {
                                ctx.LineTo(LatLonToScreen(
                                    ring[i].Lat, ring[i].Lon, width, height));
                            }
                            ctx.EndFigure(true);
                        }
                        context.DrawGeometry(null, _sectorPen, sectorGeo);
                    }

                    // Sector labels (identifier + altitude)
                    if (item.ShowLabel)
                    {
                        foreach (var labelRing in item.Rings)
                        {
                            if (labelRing.Count == 0) continue;

                            double sLat = labelRing.Average(p => p.Lat);
                            double sLon = labelRing.Average(p => p.Lon);
                            var center = LatLonToScreen(
                                sLat, sLon, width, height);

                            if (!IsOnScreen(center, width, height)) continue;

                            var idLabel = new FormattedText(
                                item.Identifier,
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                _mapLabelTypeface, 9, _mapLabelBrush);

                            var altLabel = new FormattedText(
                                item.Label,
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                _mapLabelTypeface, 9, _mapLabelBrush);

                            double totalHeight =
                                idLabel.Height + altLabel.Height + 2;

                            context.DrawText(idLabel, new Point(
                                center.X - idLabel.Width / 2,
                                center.Y - totalHeight / 2));

                            context.DrawText(altLabel, new Point(
                                center.X - altLabel.Width / 2,
                                center.Y - totalHeight / 2 +
                                    idLabel.Height + 2));
                        }
                    }
                    break;

                case "Airway":
                    if (item.Rings.Count > 0)
                    {
                        // Draw airway line
                        var airwayGeo = new StreamGeometry();
                        using (var ctx = airwayGeo.Open())
                        {
                            var ring = item.Rings[0];
                            if (ring.Count > 0)
                            {
                                var first = LatLonToScreen(
                                    ring[0].Lat, ring[0].Lon,
                                    width, height);
                                ctx.BeginFigure(first, false);
                                for (int i = 1; i < ring.Count; i++)
                                {
                                    ctx.LineTo(LatLonToScreen(
                                        ring[i].Lat, ring[i].Lon,
                                        width, height));
                                }
                                ctx.EndFigure(false);
                            }
                        }

                        var airwayPen =
                            item.Identifier.StartsWith("V",
                                StringComparison.OrdinalIgnoreCase) ||
                            item.Identifier.StartsWith("T",
                                StringComparison.OrdinalIgnoreCase)
                            ? _victorRoutePen : _jetRoutePen;

                        context.DrawGeometry(null, airwayPen, airwayGeo);

                        // Airway identifier label at midpoint
                        if (item.ShowLabel)
                        {
                            var midPt =
                                item.Rings[0][item.Rings[0].Count / 2];
                            var midScreen = LatLonToScreen(
                                midPt.Lat, midPt.Lon, width, height);

                            if (IsOnScreen(midScreen, width, height))
                            {
                                var airwayLabel = new FormattedText(
                                    item.Identifier,
                                    System.Globalization.CultureInfo
                                        .CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    _mapLabelTypeface, 9, _mapLabelBrush);

                                context.DrawText(airwayLabel, new Point(
                                    midScreen.X - airwayLabel.Width / 2,
                                    midScreen.Y - airwayLabel.Height / 2));
                            }
                        }
                    }
                    break;
            }
        }
    }

    // =========================================================================
    // Draw — TRACON boundary (special case with multi-ring + centered label)
    // =========================================================================

    private void DrawTraconBoundary(DrawingContext context,
        MapItem item, double width, double height)
    {
        foreach (var ring in item.Rings)
        {
            if (ring.Count < 2) continue;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var first = LatLonToScreen(
                    ring[0].Lat, ring[0].Lon, width, height);
                ctx.BeginFigure(first, false);

                for (int i = 1; i < ring.Count; i++)
                {
                    var pt = LatLonToScreen(
                        ring[i].Lat, ring[i].Lon, width, height);
                    ctx.LineTo(pt);
                }
                ctx.EndFigure(true);
            }

            context.DrawGeometry(null, _traconPen, geometry);
        }

        if (item.ShowLabel)
        {
            var allPoints = item.Rings.SelectMany(r => r).ToList();
            if (allPoints.Count > 0)
            {
                double avgLat = allPoints.Average(p => p.Lat);
                double avgLon = allPoints.Average(p => p.Lon);
                var center = LatLonToScreen(
                    avgLat, avgLon, width, height);

                var label = new FormattedText(
                    item.Identifier,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _mapLabelTypeface, 9, _traconBrush);

                context.DrawText(label, new Point(
                    center.X - label.Width / 2,
                    center.Y - label.Height / 2));
            }
        }
    }

    // =========================================================================
    // Draw — Lead lines (heading/speed projection from aircraft nose)
    // =========================================================================

    private void DrawLeadLines(DrawingContext context,
        double width, double height)
    {
        if (!FlightDisplaySettings.ShowLeadLines) return;

        double leadMinutes = FlightDisplaySettings.LeadLineMinutes;
        var pen = new Pen(new SolidColorBrush(Color.Parse("#888888")), 0.6)
        {
            DashStyle = DashStyle.Dash
        };

        foreach (var pilot in VisiblePilots)
        {
            if (pilot.IsHidden) continue;
            if (pilot.GroundSpeed < 30) continue; // skip stationary

            var pt = LatLonToScreen(
                pilot.Lat, pilot.Lon, width, height);
            if (!IsOnScreen(pt, width, height)) continue;

            // Project position: distance = speed × time
            double distNm = pilot.GroundSpeed * (leadMinutes / 60.0);
            double hdgRad = pilot.Heading * Math.PI / 180.0;

            // Convert NM to approximate degrees
            double dLat = (distNm / 60.0) * Math.Cos(hdgRad);
            double dLon = (distNm / 60.0) * Math.Sin(hdgRad)
                / Math.Cos(pilot.Lat * Math.PI / 180.0);

            double projLat = pilot.Lat + dLat;
            double projLon = pilot.Lon + dLon;

            var projPt = LatLonToScreen(projLat, projLon, width, height);

            context.DrawLine(pen, pt, projPt);
        }
    }
}