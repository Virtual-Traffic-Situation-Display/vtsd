using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using vTFMS.Models;

namespace vTFMS.Views;

public class TsdRadarControl : Control
{
    // Cached polyline geometries
    private Geometry? _stateBoundaryGeometry;
    private Geometry? _countryBoundaryGeometry;
    private Geometry? _sectorGeometry;

    private bool _geometriesDirty = true;

    private double _cachedCenterLat;
    private double _cachedCenterLon;
    private double _cachedZoom;
    private double _cachedWidth;
    private double _cachedHeight;

    private double _radarMinLat;
    private double _radarMinLon;
    private double _radarMaxLat;
    private double _radarMaxLon;

    private System.Threading.Timer? _radarRefreshTimer;

    private Point _currentMousePosition;

    private VatsimPilot? _hoveredPilot;

    private Avalonia.Media.Imaging.Bitmap? _radarBitmap;

    public event EventHandler? RadarRefreshRequested;

    #region Styled Properties

    public static readonly StyledProperty<ObservableCollection<StateBoundary>> StateBoundariesProperty =
        AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<StateBoundary>>(
            nameof(StateBoundaries), new ObservableCollection<StateBoundary>());

    public static readonly StyledProperty<ObservableCollection<StateBoundary>> CountryBoundariesProperty =
    AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<StateBoundary>>(
            nameof(CountryBoundaries), new ObservableCollection<StateBoundary>());

    public static readonly StyledProperty<ObservableCollection<SectorBoundary>> SectorsProperty =
        AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<SectorBoundary>>(
            nameof(Sectors), new ObservableCollection<SectorBoundary>());

    public static readonly StyledProperty<ObservableCollection<Airport>> AirportsProperty =
        AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<Airport>>(
            nameof(Airports), new ObservableCollection<Airport>());

    public static readonly StyledProperty<ObservableCollection<Navaid>> NavaidsProperty =
        AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<Navaid>>(
            nameof(Navaids), new ObservableCollection<Navaid>());

    public static readonly StyledProperty<ObservableCollection<Waypoint>> WaypointsProperty =
        AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<Waypoint>>(
            nameof(Waypoints), new ObservableCollection<Waypoint>());

    public static readonly StyledProperty<bool> ShowStateBoundariesProperty =
    AvaloniaProperty.Register<TsdRadarControl, bool>(
        nameof(ShowStateBoundaries), true);

    public static readonly StyledProperty<bool> ShowCountryBoundariesProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowCountryBoundaries), true);

    public static readonly StyledProperty<byte[]?> RadarImageDataProperty =
    AvaloniaProperty.Register<TsdRadarControl, byte[]?>(
        nameof(RadarImageData));

    public static readonly StyledProperty<bool> ShowWeatherProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowWeather), false);

    public static readonly StyledProperty<double> RadarMinLatProperty =
    AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMinLat));
    public static readonly StyledProperty<double> RadarMinLonProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMinLon));
    public static readonly StyledProperty<double> RadarMaxLatProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMaxLat));
    public static readonly StyledProperty<double> RadarMaxLonProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMaxLon));

    public double RadarMinLat
    {
        get => GetValue(RadarMinLatProperty);
        set => SetValue(RadarMinLatProperty, value);
    }
    public double RadarMinLon
    {
        get => GetValue(RadarMinLonProperty);
        set => SetValue(RadarMinLonProperty, value);
    }
    public double RadarMaxLat
    {
        get => GetValue(RadarMaxLatProperty);
        set => SetValue(RadarMaxLatProperty, value);
    }
    public double RadarMaxLon
    {
        get => GetValue(RadarMaxLonProperty);
        set => SetValue(RadarMaxLonProperty, value);
    }

    public byte[]? RadarImageData
    {
        get => GetValue(RadarImageDataProperty);
        set => SetValue(RadarImageDataProperty, value);
    }

    public bool ShowWeather
    {
        get => GetValue(ShowWeatherProperty);
        set => SetValue(ShowWeatherProperty, value);
    }

    public bool ShowStateBoundaries
    {
        get => GetValue(ShowStateBoundariesProperty);
        set => SetValue(ShowStateBoundariesProperty, value);
    }

    public bool ShowCountryBoundaries
    {
        get => GetValue(ShowCountryBoundariesProperty);
        set => SetValue(ShowCountryBoundariesProperty, value);
    }

    public static readonly StyledProperty<double> CenterLatProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(CenterLat), 39.5);

    public static readonly StyledProperty<double> CenterLonProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(CenterLon), -98.35);

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<bool> ShowAirportsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(nameof(ShowAirports), true);

    public static readonly StyledProperty<bool> ShowWaypointsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(nameof(ShowWaypoints), false);

    public static readonly StyledProperty<bool> ShowVorsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(nameof(ShowVors), true);

    public static readonly StyledProperty<bool> ShowNdbsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(nameof(ShowNdbs), true);

    public static readonly StyledProperty<ObservableCollection<MapItem>> ActiveMapItemsProperty =
        AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<MapItem>>(
        nameof(ActiveMapItems), new ObservableCollection<MapItem>());

    public static readonly StyledProperty<ObservableCollection<VatsimPilot>> VisiblePilotsProperty =
    AvaloniaProperty.Register<TsdRadarControl, ObservableCollection<VatsimPilot>>(
        nameof(VisiblePilots), new ObservableCollection<VatsimPilot>());

    public ObservableCollection<VatsimPilot> VisiblePilots
    {
        get => GetValue(VisiblePilotsProperty);
        set => SetValue(VisiblePilotsProperty, value);
    }

    public ObservableCollection<MapItem> ActiveMapItems
    {
        get => GetValue(ActiveMapItemsProperty);
        set => SetValue(ActiveMapItemsProperty, value);
    }

    public ObservableCollection<StateBoundary> StateBoundaries
    {
        get => GetValue(StateBoundariesProperty);
        set => SetValue(StateBoundariesProperty, value);
    }
    public ObservableCollection<StateBoundary> CountryBoundaries
    {
        get => GetValue(CountryBoundariesProperty);
        set => SetValue(CountryBoundariesProperty, value);
    }

    public ObservableCollection<SectorBoundary> Sectors
    {
        get => GetValue(SectorsProperty);
        set => SetValue(SectorsProperty, value);
    }

    public ObservableCollection<Airport> Airports
    {
        get => GetValue(AirportsProperty);
        set => SetValue(AirportsProperty, value);
    }

    public ObservableCollection<Navaid> Navaids
    {
        get => GetValue(NavaidsProperty);
        set => SetValue(NavaidsProperty, value);
    }

    public ObservableCollection<Waypoint> Waypoints
    {
        get => GetValue(WaypointsProperty);
        set => SetValue(WaypointsProperty, value);
    }

    public double CenterLat
    {
        get => GetValue(CenterLatProperty);
        set => SetValue(CenterLatProperty, value);
    }

    public double CenterLon
    {
        get => GetValue(CenterLonProperty);
        set => SetValue(CenterLonProperty, value);
    }

    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public bool ShowAirports
    {
        get => GetValue(ShowAirportsProperty);
        set => SetValue(ShowAirportsProperty, value);
    }

    public bool ShowWaypoints
    {
        get => GetValue(ShowWaypointsProperty);
        set => SetValue(ShowWaypointsProperty, value);
    }

    public bool ShowVors
    {
        get => GetValue(ShowVorsProperty);
        set => SetValue(ShowVorsProperty, value);
    }

    public bool ShowNdbs
    {
        get => GetValue(ShowNdbsProperty);
        set => SetValue(ShowNdbsProperty, value);
    }

    #endregion

    static TsdRadarControl()
    {
        AffectsRender<TsdRadarControl>(
            StateBoundariesProperty,
            CountryBoundariesProperty,
            SectorsProperty,
            AirportsProperty,
            NavaidsProperty,
            WaypointsProperty,
            CenterLatProperty,
            CenterLonProperty,
            ZoomLevelProperty,
            ShowAirportsProperty,
            ShowWaypointsProperty,
            ShowVorsProperty,
            ShowNdbsProperty,
            ActiveMapItemsProperty,
            VisiblePilotsProperty,
            ShowStateBoundariesProperty,
            ShowCountryBoundariesProperty,
            RadarImageDataProperty,
            ShowWeatherProperty,
            DisplaySettingsProperty,
            RadarOpacityProperty);
    }

    public TsdRadarControl()
    {
        Focusable = true;
    }

    public static readonly StyledProperty<DisplaySettings>
    DisplaySettingsProperty =
    AvaloniaProperty.Register<TsdRadarControl, DisplaySettings>(
        nameof(DisplaySettings), new DisplaySettings());

    public DisplaySettings DisplaySettings
    {
        get => GetValue(DisplaySettingsProperty);
        set => SetValue(DisplaySettingsProperty, value);
    }

    public static readonly StyledProperty<double> RadarOpacityProperty =
    AvaloniaProperty.Register<TsdRadarControl, double>(
        nameof(RadarOpacity), 0.7);

    public double RadarOpacity
    {
        get => GetValue(RadarOpacityProperty);
        set => SetValue(RadarOpacityProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateBoundariesProperty ||
            change.Property == CountryBoundariesProperty ||
            change.Property == SectorsProperty ||
            change.Property == AirportsProperty ||
            change.Property == NavaidsProperty ||
            change.Property == WaypointsProperty ||
            change.Property == CenterLatProperty ||
            change.Property == CenterLonProperty ||
            change.Property == ZoomLevelProperty ||
            change.Property == ActiveMapItemsProperty ||
            change.Property == VisiblePilotsProperty ||
            change.Property == ShowStateBoundariesProperty ||
            change.Property == ShowCountryBoundariesProperty ||
            change.Property == DisplaySettingsProperty)
        {
            _geometriesDirty = true;
            InvalidateVisual();

            if (change.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += (_, _) =>
                {
                    _geometriesDirty = true;
                    InvalidateVisual();
                };
        }
        if (change.Property == RadarImageDataProperty)
        {
            if (change.NewValue is byte[] data && data.Length > 0)
            {
                try
                {
                    using var ms = new System.IO.MemoryStream(data);
                    _radarBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"TsdRadarControl: bitmap error — {ex.Message}");
                    _radarBitmap = null;
                }
            }
            else
            {
                _radarBitmap = null;
            }
            InvalidateVisual();
        }
    }

    private Point LatLonToScreen(double lat, double lon,
                                  double width, double height)
    {
        double scale = Math.Min(width, height) * 0.45 * ZoomLevel;
        double x = width / 2 + (lon - CenterLon) * scale / 57.0;
        double y = height / 2 - (lat - CenterLat) * scale / 57.0;
        return new Point(x, y);
    }

    private bool IsOnScreen(Point pt, double width, double height)
    {
        return pt.X >= -20 && pt.X <= width + 20 &&
               pt.Y >= -20 && pt.Y <= height + 20;
    }

    private void RebuildGeometries(double width, double height)
    {
        // Rebuild polyline geometries
        _stateBoundaryGeometry = BuildPolylineGeometry(
            StateBoundaries.Select(b => b.Points).ToList(),
            width, height);

        _countryBoundaryGeometry = BuildPolylineGeometry(
            CountryBoundaries.Select(b => b.Points).ToList(),
            width, height);

        _sectorGeometry = BuildPolylineGeometry(
            Sectors.Select(s => s.Points).ToList(),
            width, height);

        _cachedCenterLat = CenterLat;
        _cachedCenterLon = CenterLon;
        _cachedZoom = ZoomLevel;
        _cachedWidth = width;
        _cachedHeight = height;
        _geometriesDirty = false;
    }

    private Geometry BuildPolylineGeometry(
        List<List<(double Lat, double Lon)>> polylines,
        double width, double height)
    {
        var geo = new StreamGeometry();
        using var ctx = geo.Open();

        foreach (var line in polylines)
        {
            if (line.Count < 2) continue;
            var first = LatLonToScreen(
                line[0].Lat, line[0].Lon, width, height);
            ctx.BeginFigure(first, false);
            for (int i = 1; i < line.Count; i++)
            {
                var pt = LatLonToScreen(
                    line[i].Lat, line[i].Lon, width, height);
                ctx.LineTo(pt);
            }
            ctx.EndFigure(false);
        }

        return geo;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _currentMousePosition = e.GetPosition(this);

        // Check if hovering over a pilot
        var prev = _hoveredPilot;
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
                break;
            }
        }

        if (_hoveredPilot != prev)
            InvalidateVisual();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var width = Bounds.Width;
        var height = Bounds.Height;

        switch (e.Key)
        {
            case Key.M:
                double scale = Math.Min(width, height) * 0.45 * ZoomLevel;
                CenterLon = CenterLon +
                    (_currentMousePosition.X - width / 2) * 57.0 / scale;
                CenterLat = CenterLat -
                    (_currentMousePosition.Y - height / 2) * 57.0 / scale;
                e.Handled = true;
                ScheduleRadarRefresh();
                break;

            case Key.Z:
                ZoomLevel = Math.Clamp(ZoomLevel * 1.25, 0.25, 20.0);
                e.Handled = true;
                ScheduleRadarRefresh();
                break;

            case Key.U:
                ZoomLevel = Math.Clamp(ZoomLevel / 1.25, 0.25, 20.0);
                e.Handled = true;
                ScheduleRadarRefresh();
                break;
        }
    }

    private void DrawAircraft(DrawingContext context,
                           double width, double height)
    {
        var typeface = new Typeface("Courier New");
        var dataBlockBg = new SolidColorBrush(Color.Parse("#000000"));
        var dataBlockBorder = new Pen(
            new SolidColorBrush(Color.Parse("#FFFFFF")), 0.5);
        const double size = 4.0;

        foreach (var pilot in VisiblePilots)
        {
            var pt = LatLonToScreen(pilot.Lat, pilot.Lon, width, height);
            if (!IsOnScreen(pt, width, height)) continue;

            var brush = new SolidColorBrush(
                Color.Parse(pilot.MatchedFilterColor));

            DrawAircraftSymbol(context, pt, pilot.Heading, size, brush);

            if (_hoveredPilot == pilot)
                DrawDataBlock(context, pt, pilot, brush, typeface);
        }
    }

    private void DrawAircraftSymbol(DrawingContext context,
    Point pt, int heading, double size, IBrush brush)
    {
        double scale = size / 6.0;
        double rad = heading * Math.PI / 180.0;

        // Helper to rotate and translate a point
        Point Transform(double x, double y)
        {
            double rx = x * Math.Cos(rad) - y * Math.Sin(rad);
            double ry = x * Math.Sin(rad) + y * Math.Cos(rad);
            return new Point(pt.X + rx * scale, pt.Y + ry * scale);
        }

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            // Fuselage nose to tail
            ctx.BeginFigure(Transform(0, -6.5), true);

            // Right side of nose
            ctx.LineTo(Transform(1, -8));

            // Right main wing leading edge
            ctx.LineTo(Transform(1, -2));
            ctx.LineTo(Transform(8, 4));   // wing tip

            // Right main wing trailing edge
            ctx.LineTo(Transform(6, 5));
            ctx.LineTo(Transform(1, 1));

            // Right side to tail
            ctx.LineTo(Transform(1, 6));

            // Right horizontal stabilizer
            ctx.LineTo(Transform(4, 9));
            ctx.LineTo(Transform(4, 10));
            ctx.LineTo(Transform(1, 8));

            // Tail center
            ctx.LineTo(Transform(0, 8));

            // Left horizontal stabilizer
            ctx.LineTo(Transform(-1, 8));
            ctx.LineTo(Transform(-4, 10));
            ctx.LineTo(Transform(-4, 9));
            ctx.LineTo(Transform(-1, 6));

            // Left side to wing
            ctx.LineTo(Transform(-1, 1));

            // Left main wing trailing edge
            ctx.LineTo(Transform(-6, 5));
            ctx.LineTo(Transform(-8, 4));  // wing tip

            // Left main wing leading edge
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

        var lines = new[]
        {
        pilot.Callsign,
        $"{pilot.AircraftType,-4} {altStr}",
        $"{pilot.GroundSpeed}",
        pilot.Arrival
    };

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

            context.DrawText(text,
                new Point(bx, by + i * lineHeight));
        }
    }

    private void DrawTraconBoundary(DrawingContext context,
    MapItem item, double width, double height)
    {
        var color = DisplaySettings.TraconColor;
        var pen = new Pen(
            new SolidColorBrush(Avalonia.Media.Color.Parse(color)), 1.0);
        var typeface = new Typeface("Courier New");
        var brush = new SolidColorBrush(
            Avalonia.Media.Color.Parse(color));

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

            context.DrawGeometry(null, pen, geometry);
        }

        // Draw label at centroid
        var allPoints = item.Rings.SelectMany(r => r).ToList();
        if (allPoints.Count > 0)
        {
            double avgLat = allPoints.Average(p => p.Lat);
            double avgLon = allPoints.Average(p => p.Lon);
            var center = LatLonToScreen(avgLat, avgLon, width, height);

            var label = new FormattedText(
                item.Identifier,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface, 9, brush);

            context.DrawText(label, new Point(
                center.X - label.Width / 2,
                center.Y - label.Height / 2));
        }
    }

    private void ScheduleRadarRefresh()
    {
        if (!ShowWeather) return;

        System.Diagnostics.Debug.WriteLine(
            "TsdRadarControl: scheduling radar refresh in 5s");

        _radarRefreshTimer?.Dispose();
        _radarRefreshTimer = new System.Threading.Timer(_ =>
        {
            System.Diagnostics.Debug.WriteLine(
                "TsdRadarControl: radar refresh timer fired");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                RadarRefreshRequested?.Invoke(this, EventArgs.Empty));
        }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        // Background
        context.FillRectangle(
            new SolidColorBrush(
                Avalonia.Media.Color.Parse(
                    DisplaySettings.BackgroundColor)),
            new Rect(0, 0, width, height));

        // Rebuild if dirty
        if (_geometriesDirty ||
            Math.Abs(_cachedWidth - width) > 0.5 ||
            Math.Abs(_cachedHeight - height) > 0.5)
        {
            RebuildGeometries(width, height);
        }

        // State boundaries
        if (ShowStateBoundaries && _stateBoundaryGeometry != null)
            context.DrawGeometry(null,
                new Pen(new SolidColorBrush(
                    Avalonia.Media.Color.Parse(
                        DisplaySettings.BoundaryColor)), 0.8),
                _stateBoundaryGeometry);

        // Country boundaries
        if (ShowCountryBoundaries && _countryBoundaryGeometry != null)
            context.DrawGeometry(null,
                new Pen(new SolidColorBrush(
                    Avalonia.Media.Color.Parse(
                        DisplaySettings.BoundaryColor)), 0.8),
                _countryBoundaryGeometry);

        // Sector boundaries
        if (_sectorGeometry != null)
            context.DrawGeometry(null,
                new Pen(new SolidColorBrush(Color.Parse("#CC0000")), 1.0),
                _sectorGeometry);

        // Radar overlay
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

        DrawActiveMapItems(context, width, height);

        // Aircraft
        DrawAircraft(context, width, height);
    }

    private void DrawActiveMapItems(DrawingContext context,
                                 double width, double height)
    {
        var airportBrush = new SolidColorBrush(
            Avalonia.Media.Color.Parse(DisplaySettings.AirportColor));
        var vorBrush = new SolidColorBrush(
            Avalonia.Media.Color.Parse(DisplaySettings.VorColor));
        var ndbBrush = new SolidColorBrush(
            Avalonia.Media.Color.Parse(DisplaySettings.NdbColor));
        var fixBrush = new SolidColorBrush(
            Avalonia.Media.Color.Parse(DisplaySettings.FixColor));
        var vorPen = new Pen(vorBrush, 1.0);
        var ndbPen = new Pen(ndbBrush, 1.0);
        var fixPen = new Pen(fixBrush, 0.8);
        var typeface = new Typeface("Courier New");
        const double half = 2.5;
        const double triSize = 5.0;

        foreach (var item in ActiveMapItems)
        {
            var pt = LatLonToScreen(item.Lat, item.Lon, width, height);
            if (!IsOnScreen(pt, width, height)) continue;

            switch (item.Type)
            {
                case "Airport":
                    // Filled 5x5 cyan box
                    context.FillRectangle(airportBrush,
                        new Rect(pt.X - half, pt.Y - half,
                                 half * 2, half * 2));

                    var airportLabel = new FormattedText(
                        item.Identifier.Length > 3
                            ? item.Identifier[..3]
                            : item.Identifier,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface, 9, airportBrush);

                    context.DrawText(airportLabel,
                        new Point(pt.X + half + 2,
                                  pt.Y - airportLabel.Height / 2));
                    break;

                case string t when t.Contains("VOR"):
                    // Stretched ellipse — orange
                    context.DrawEllipse(null, vorPen, pt, 2.5, 4.0);

                    var vorLabel = new FormattedText(
                        item.Identifier.Length > 3
                            ? item.Identifier[..3]
                            : item.Identifier,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface, 9, vorBrush);

                    context.DrawText(vorLabel,
                        new Point(pt.X + 6,
                                  pt.Y - vorLabel.Height / 2));
                    break;

                case string t when t.Contains("NDB"):
                    // Stretched ellipse — magenta
                    context.DrawEllipse(null, ndbPen, pt, 2.5, 4.0);

                    var ndbLabel = new FormattedText(
                        item.Identifier.Length > 3
                            ? item.Identifier[..3]
                            : item.Identifier,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface, 9, ndbBrush);

                    context.DrawText(ndbLabel,
                        new Point(pt.X + 6,
                                  pt.Y - ndbLabel.Height / 2));
                    break;

                case "Fix":
                    // White triangle (Δ)
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
                    context.DrawGeometry(null, fixPen, geo);

                    var fixLabel = new FormattedText(
                        item.Identifier.Length > 5
                            ? item.Identifier[..5]
                            : item.Identifier,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface, 9, fixBrush);

                    context.DrawText(fixLabel,
                        new Point(pt.X + triSize + 2,
                                  pt.Y - fixLabel.Height / 2));
                    break;

                case "TRACON":
                    DrawTraconBoundary(context, item, width, height);
                    break;
            }
        }
    }
}