using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using vTFMS.Models;

namespace vTFMS.Views;

public partial class TsdRadarControl
{
    // =========================================================================
    // Input — Pointer (hover, right-click context menus)
    // =========================================================================

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _currentMousePosition = e.GetPosition(this);

        var prevCallsign = _hoveredCallsign;
        _hoveredCallsign = null;
        _hoveredPilot = null;

        foreach (var pilot in VisiblePilots)
        {
            var pt = LatLonToScreen(pilot.Lat, pilot.Lon,
                Bounds.Width, Bounds.Height);
            double dx = _currentMousePosition.X - pt.X;
            double dy = _currentMousePosition.Y - pt.Y;

            if (Math.Sqrt(dx * dx + dy * dy) <= 8.0)
            {
                _hoveredPilot = pilot;
                _hoveredCallsign = pilot.Callsign;
                break;
            }
        }

        if (_hoveredCallsign != prevCallsign)
            InvalidateVisual();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Focus();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsRightButtonPressed) return;

        var clickPos = point.Position;
        var width = Bounds.Width;
        var height = Bounds.Height;

        // ── Hit-test 1: Flight icons ─────────────────────────
        foreach (var pilot in VisiblePilots)
        {
            if (pilot.IsHidden) continue;
            var pt = LatLonToScreen(pilot.Lat, pilot.Lon, width, height);
            double dx = clickPos.X - pt.X;
            double dy = clickPos.Y - pt.Y;

            if (Math.Sqrt(dx * dx + dy * dy) <= 10.0)
            {
                ShowFlightContextMenu(pilot);
                e.Handled = true;
                return;
            }
        }

        // ── Hit-test 2: Active map items ─────────────────────
        foreach (var item in ActiveMapItems)
        {
            var pt = LatLonToScreen(item.Lat, item.Lon, width, height);
            double dx = clickPos.X - pt.X;
            double dy = clickPos.Y - pt.Y;

            if (Math.Sqrt(dx * dx + dy * dy) <= 12.0)
            {
                ShowMapItemContextMenu(item);
                e.Handled = true;
                return;
            }
        }

        // ── Hit-test 3: Empty space (generic) ────────────────
        ShowGenericContextMenu(clickPos);
        e.Handled = true;
    }

    // =========================================================================
    // Context menus
    // =========================================================================

    /// <summary>Context menu for right-clicking a flight icon.</summary>
    private void ShowFlightContextMenu(VatsimPilot pilot)
    {
        var menu = new ContextMenu();

        var dataBlockItem = new MenuItem
        {
            Header = pilot.ShowDataBlock
                ? "Hide Data Block" : "Show Data Block"
        };
        dataBlockItem.Click += (_, _) =>
        {
            pilot.ShowDataBlock = !pilot.ShowDataBlock;
            InvalidateVisual();
        };
        menu.Items.Add(dataBlockItem);

        var orgDestItem = new MenuItem
        {
            Header = pilot.ShowOrgDest
                ? "Hide Org/Dest" : "Show Org/Dest"
        };
        orgDestItem.Click += (_, _) =>
        {
            pilot.ShowOrgDest = !pilot.ShowOrgDest;
            InvalidateVisual();
        };
        menu.Items.Add(orgDestItem);

        var drawRouteItem = new MenuItem
        {
            Header = (pilot.ManualDrawRoute || pilot.MatchedDrawRoute)
                ? "Hide Route" : "Draw Route"
        };
        drawRouteItem.Click += (_, _) =>
        {
            pilot.ManualDrawRoute = !pilot.ManualDrawRoute;
            if (pilot.ManualDrawRoute &&
                pilot.ParsedRoute.Count == 0 &&
                !string.IsNullOrWhiteSpace(pilot.Route))
            {
                RouteResolveRequested?.Invoke(this, pilot);
            }
            InvalidateVisual();
        };
        menu.Items.Add(drawRouteItem);

        var showRouteItem = new MenuItem
        {
            Header = (pilot.ManualShowRoute || pilot.MatchedShowRoute)
                ? "Hide Route Text" : "Show Route Text"
        };
        showRouteItem.Click += (_, _) =>
        {
            pilot.ManualShowRoute = !pilot.ManualShowRoute;
            InvalidateVisual();
        };
        menu.Items.Add(showRouteItem);

        // Color submenu
        var colorMenu = new MenuItem { Header = "Change Color" };
        var colors = new Dictionary<string, string>
        {
            { "White",   "#FFFFFF" },
            { "Cyan",    "#00FFFF" },
            { "Green",   "#00FF00" },
            { "Yellow",  "#FFFF00" },
            { "Orange",  "#FFA500" },
            { "Red",     "#FF0000" },
            { "Magenta", "#FF00FF" },
            { "Reset",   "" }
        };

        foreach (var (name, hex) in colors)
        {
            var colorItem = new MenuItem { Header = name };
            var capturedHex = hex;
            colorItem.Click += (_, _) =>
            {
                pilot.ColorOverride =
                    string.IsNullOrEmpty(capturedHex) ? null : capturedHex;
                InvalidateVisual();
            };
            colorMenu.Items.Add(colorItem);
        }
        menu.Items.Add(colorMenu);

        menu.Items.Add(new Separator());

        var deleteItem = new MenuItem { Header = "Delete Icon" };
        deleteItem.Click += (_, _) =>
        {
            pilot.IsHidden = true;
            InvalidateVisual();
        };
        menu.Items.Add(deleteItem);

        this.ContextMenu = menu;
        menu.Open(this);
    }

    /// <summary>Context menu for right-clicking an active map item.</summary>
    private void ShowMapItemContextMenu(MapItem item)
    {
        var menu = new ContextMenu();

        var labelItem = new MenuItem
        {
            Header = item.ShowLabel ? "Hide Label" : "Show Label"
        };
        labelItem.Click += (_, _) =>
        {
            item.ShowLabel = !item.ShowLabel;
            InvalidateVisual();
        };
        menu.Items.Add(labelItem);

        var centerItem = new MenuItem { Header = "Center On" };
        centerItem.Click += (_, _) =>
        {
            _prevCenterLat = CenterLat;
            _prevCenterLon = CenterLon;
            _prevZoomLevel = ZoomLevel;

            CenterLat = item.Lat;
            CenterLon = item.Lon;
            ScheduleRadarRefresh();
        };
        menu.Items.Add(centerItem);

        menu.Items.Add(new Separator());

        var removeItem = new MenuItem { Header = "Remove Item" };
        removeItem.Click += (_, _) =>
        {
            MapItemRemoveRequested?.Invoke(this, item);
        };
        menu.Items.Add(removeItem);

        this.ContextMenu = menu;
        menu.Open(this);
    }

    /// <summary>Context menu for right-clicking empty radar space.</summary>
    private void ShowGenericContextMenu(Avalonia.Point clickPos)
    {
        var menu = new ContextMenu();

        var centerItem = new MenuItem { Header = "Center Here" };
        centerItem.Click += (_, _) =>
        {
            _prevCenterLat = CenterLat;
            _prevCenterLon = CenterLon;
            _prevZoomLevel = ZoomLevel;

            var width = Bounds.Width;
            var height = Bounds.Height;
            double scale = Math.Min(width, height) * 0.45 * ZoomLevel;

            double dxProj = (clickPos.X - width / 2.0)
                * (Math.PI / 180.0) * 57.0 / scale;
            double dyProj = (clickPos.Y - height / 2.0)
                * (Math.PI / 180.0) * 57.0 / scale;

            var (newLat, newLon) = LccInverse(
                dxProj, -dyProj, CenterLat, CenterLon);

            CenterLat = newLat;
            CenterLon = newLon;
            ScheduleRadarRefresh();
        };
        menu.Items.Add(centerItem);

        var undoItem = new MenuItem { Header = "Undo Move/Zoom" };
        undoItem.Click += (_, _) =>
        {
            var tempLat = CenterLat;
            var tempLon = CenterLon;
            var tempZoom = ZoomLevel;

            CenterLat = _prevCenterLat;
            CenterLon = _prevCenterLon;
            ZoomLevel = _prevZoomLevel;

            _prevCenterLat = tempLat;
            _prevCenterLon = tempLon;
            _prevZoomLevel = tempZoom;

            ScheduleRadarRefresh();
        };
        menu.Items.Add(undoItem);

        menu.Items.Add(new Separator());

        var selectFlightsItem = new MenuItem { Header = "Select Flights..." };
        selectFlightsItem.Click += (_, _) =>
            GenericMenuCommandRequested?.Invoke(this, "SelectFlights");
        menu.Items.Add(selectFlightsItem);

        var showMapItem = new MenuItem { Header = "Show Map Item..." };
        showMapItem.Click += (_, _) =>
            GenericMenuCommandRequested?.Invoke(this, "ShowMapItem");
        menu.Items.Add(showMapItem);

        var rangeRingsItem = new MenuItem { Header = "Range Rings..." };
        rangeRingsItem.Click += (_, _) =>
            GenericMenuCommandRequested?.Invoke(this, "RangeRings");
        menu.Items.Add(rangeRingsItem);

        this.ContextMenu = menu;
        menu.Open(this);
    }

    // =========================================================================
    // Input — Keyboard (quick keys)
    // =========================================================================

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var width = Bounds.Width;
        var height = Bounds.Height;

        switch (e.Key)
        {
            case Key.A:
                ShowArtcc = !ShowArtcc;
                e.Handled = true;
                break;

            case Key.B:
                bool boundariesOn = ShowStateBoundaries || ShowCountryBoundaries;
                ShowStateBoundaries = !boundariesOn;
                ShowCountryBoundaries = !boundariesOn;
                e.Handled = true;
                break;

            case Key.D:
                _geometriesDirty = true;
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.F:
                ShowAllAircraft = !ShowAllAircraft;
                e.Handled = true;
                break;

            case Key.I:
                _prevCenterLat = CenterLat;
                _prevCenterLon = CenterLon;
                _prevZoomLevel = ZoomLevel;

                CenterLat = 38.5;
                CenterLon = -96.0;
                ZoomLevel = 4.0;
                ShowStateBoundaries = true;
                ShowCountryBoundaries = true;

                e.Handled = true;
                ScheduleRadarRefresh();
                break;

            case Key.M:
                _prevCenterLat = CenterLat;
                _prevCenterLon = CenterLon;
                _prevZoomLevel = ZoomLevel;

                double scale = Math.Min(width, height) * 0.45 * ZoomLevel;

                double dxProj = (_currentMousePosition.X - width / 2.0)
                    * (Math.PI / 180.0) * 57.0 / scale;
                double dyProj = (_currentMousePosition.Y - height / 2.0)
                    * (Math.PI / 180.0) * 57.0 / scale;

                var (newLat, newLon) = LccInverse(
                    dxProj, -dyProj, CenterLat, CenterLon);

                CenterLat = newLat;
                CenterLon = newLon;
                e.Handled = true;
                ScheduleRadarRefresh();
                break;

            case Key.U:
                _prevCenterLat = CenterLat;
                _prevCenterLon = CenterLon;
                _prevZoomLevel = ZoomLevel;

                ZoomLevel = Math.Clamp(ZoomLevel / 1.25, 0.25, 20.0);
                e.Handled = true;
                ScheduleRadarRefresh();
                break;

            case Key.W:
                ShowWeather = !ShowWeather;
                if (ShowWeather)
                    ScheduleRadarRefresh();
                e.Handled = true;
                break;

            case Key.X:
                var tempLat = CenterLat;
                var tempLon = CenterLon;
                var tempZoom = ZoomLevel;

                CenterLat = _prevCenterLat;
                CenterLon = _prevCenterLon;
                ZoomLevel = _prevZoomLevel;

                _prevCenterLat = tempLat;
                _prevCenterLon = tempLon;
                _prevZoomLevel = tempZoom;

                e.Handled = true;
                ScheduleRadarRefresh();
                break;

            case Key.Z:
                _prevCenterLat = CenterLat;
                _prevCenterLon = CenterLon;
                _prevZoomLevel = ZoomLevel;

                ZoomLevel = Math.Clamp(ZoomLevel * 1.25, 0.25, 20.0);
                e.Handled = true;
                ScheduleRadarRefresh();
                break;
        }
    }
}