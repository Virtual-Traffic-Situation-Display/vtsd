using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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
    // Lambert Conformal Conic standard parallels for US
    private const double Phi1 = 33.0 * Math.PI / 180.0;
    private const double Phi2 = 45.0 * Math.PI / 180.0;

    // Cached polyline geometries
    private Geometry? _stateBoundaryGeometry;
    private Geometry? _countryBoundaryGeometry;
    private Geometry? _sectorGeometry;
    private Geometry? _artccBoundaryGeometry;

    private bool _geometriesDirty = true;

    private double _cachedCenterLat;
    private double _cachedCenterLon;
    private double _cachedZoom;
    private double _cachedWidth;
    private double _cachedHeight;

    private System.Threading.Timer? _radarRefreshTimer;
    private double _prevCenterLat;
    private double _prevCenterLon;
    private double _prevZoomLevel;
    private Point _currentMousePosition;
    private VatsimPilot? _hoveredPilot;
    private string? _hoveredCallsign;

    private Avalonia.Media.Imaging.Bitmap? _radarBitmap;

    // Cached render objects
    private SolidColorBrush _backgroundBrush = new(Colors.Black);
    private Pen _boundaryPen = new(new SolidColorBrush(Colors.White), 0.8);
    private Pen _sectorPen = new(new SolidColorBrush(Colors.Gray), 1.0);
    private Pen _artccPen = new(new SolidColorBrush(Colors.Red), 1.5);
    private Pen _jetRoutePen = new(new SolidColorBrush(Colors.Cyan), 0.8);
    private Pen _victorRoutePen = new(new SolidColorBrush(Colors.Green), 0.8);
    private SolidColorBrush _airportBrush = new(Colors.Cyan);
    private Pen _vorPen = new(new SolidColorBrush(Colors.Orange), 1.0);
    private SolidColorBrush _vorBrush = new(Colors.Orange);
    private Pen _ndbPen = new(new SolidColorBrush(Colors.Magenta), 1.0);
    private SolidColorBrush _ndbBrush = new(Colors.Magenta);
    private Pen _fixPen = new(new SolidColorBrush(Colors.White), 0.8);
    private SolidColorBrush _fixBrush = new(Colors.White);
    private SolidColorBrush _traconBrush = new(Colors.Cyan);
    private Pen _traconPen = new(new SolidColorBrush(Colors.Cyan), 1.0);
    private Typeface _dataBlockTypeface = new("Courier New");
    private Typeface _mapLabelTypeface = new("Courier New");
    private SolidColorBrush _dataBlockBrush = new(Colors.Cyan);
    private SolidColorBrush _mapLabelBrush = new(Colors.Cyan);

    public event EventHandler? RadarRefreshRequested;

    #region Styled Properties

    public static readonly StyledProperty<ObservableCollection<StateBoundary>>
        StateBoundariesProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<StateBoundary>>(
            nameof(StateBoundaries),
            new ObservableCollection<StateBoundary>());

    public static readonly StyledProperty<ObservableCollection<StateBoundary>>
        CountryBoundariesProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<StateBoundary>>(
            nameof(CountryBoundaries),
            new ObservableCollection<StateBoundary>());

    public static readonly StyledProperty<ObservableCollection<SectorBoundary>>
        SectorsProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<SectorBoundary>>(
            nameof(Sectors),
            new ObservableCollection<SectorBoundary>());

    public static readonly StyledProperty<ObservableCollection<Airport>>
        AirportsProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<Airport>>(
            nameof(Airports),
            new ObservableCollection<Airport>());

    public static readonly StyledProperty<ObservableCollection<Navaid>>
        NavaidsProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<Navaid>>(
            nameof(Navaids),
            new ObservableCollection<Navaid>());

    public static readonly StyledProperty<ObservableCollection<Waypoint>>
        WaypointsProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<Waypoint>>(
            nameof(Waypoints),
            new ObservableCollection<Waypoint>());

    public static readonly StyledProperty<ObservableCollection<MapItem>>
        ActiveMapItemsProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<MapItem>>(
            nameof(ActiveMapItems),
            new ObservableCollection<MapItem>());

    public static readonly StyledProperty<ObservableCollection<VatsimPilot>>
        VisiblePilotsProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<VatsimPilot>>(
            nameof(VisiblePilots),
            new ObservableCollection<VatsimPilot>());

    public static readonly StyledProperty<ObservableCollection<ArtccBoundary>>
        ArtccBoundariesProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<ArtccBoundary>>(
            nameof(ArtccBoundaries),
            new ObservableCollection<ArtccBoundary>());

    public static readonly StyledProperty<bool> ShowStateBoundariesProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowStateBoundaries), true);

    public static readonly StyledProperty<bool> ShowCountryBoundariesProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowCountryBoundaries), true);

    public static readonly StyledProperty<bool> ShowArtccProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowArtcc), false);

    public static readonly StyledProperty<bool> ShowWeatherProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowWeather), false);

    public static readonly StyledProperty<byte[]?> RadarImageDataProperty =
        AvaloniaProperty.Register<TsdRadarControl, byte[]?>(
            nameof(RadarImageData));

    public static readonly StyledProperty<double> RadarMinLatProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMinLat));

    public static readonly StyledProperty<double> RadarMinLonProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMinLon));

    public static readonly StyledProperty<double> RadarMaxLatProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMaxLat));

    public static readonly StyledProperty<double> RadarMaxLonProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(nameof(RadarMaxLon));

    public static readonly StyledProperty<double> RadarOpacityProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(
            nameof(RadarOpacity), 0.7);

    public static readonly StyledProperty<double> CenterLatProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(
            nameof(CenterLat), 39.5);

    public static readonly StyledProperty<double> CenterLonProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(
            nameof(CenterLon), -98.35);

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<TsdRadarControl, double>(
            nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<bool> ShowAirportsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowAirports), true);

    public static readonly StyledProperty<bool> ShowWaypointsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowWaypoints), false);

    public static readonly StyledProperty<bool> ShowVorsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowVors), true);

    public static readonly StyledProperty<bool> ShowNdbsProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowNdbs), true);

    public static readonly StyledProperty<DisplaySettings>
        DisplaySettingsProperty =
        AvaloniaProperty.Register<TsdRadarControl, DisplaySettings>(
            nameof(DisplaySettings), new DisplaySettings());

    public static readonly StyledProperty<ObservableCollection<RangeRingConfig>>
        RangeRingsProperty =
        AvaloniaProperty.Register<TsdRadarControl,
            ObservableCollection<RangeRingConfig>>(
            nameof(RangeRings),
            new ObservableCollection<RangeRingConfig>());

    public static readonly StyledProperty<bool> ShowAllAircraftProperty =
        AvaloniaProperty.Register<TsdRadarControl, bool>(
            nameof(ShowAllAircraft), false);

    #endregion

    #region CLR Property Wrappers

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

    public ObservableCollection<MapItem> ActiveMapItems
    {
        get => GetValue(ActiveMapItemsProperty);
        set => SetValue(ActiveMapItemsProperty, value);
    }

    public ObservableCollection<VatsimPilot> VisiblePilots
    {
        get => GetValue(VisiblePilotsProperty);
        set => SetValue(VisiblePilotsProperty, value);
    }

    public ObservableCollection<ArtccBoundary> ArtccBoundaries
    {
        get => GetValue(ArtccBoundariesProperty);
        set => SetValue(ArtccBoundariesProperty, value);
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

    public bool ShowArtcc
    {
        get => GetValue(ShowArtccProperty);
        set => SetValue(ShowArtccProperty, value);
    }

    public bool ShowWeather
    {
        get => GetValue(ShowWeatherProperty);
        set => SetValue(ShowWeatherProperty, value);
    }

    public byte[]? RadarImageData
    {
        get => GetValue(RadarImageDataProperty);
        set => SetValue(RadarImageDataProperty, value);
    }

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

    public double RadarOpacity
    {
        get => GetValue(RadarOpacityProperty);
        set => SetValue(RadarOpacityProperty, value);
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

    public DisplaySettings DisplaySettings
    {
        get => GetValue(DisplaySettingsProperty);
        set => SetValue(DisplaySettingsProperty, value);
    }

    public ObservableCollection<RangeRingConfig> RangeRings
    {
        get => GetValue(RangeRingsProperty);
        set => SetValue(RangeRingsProperty, value);
    }

    public bool ShowAllAircraft
    {
        get => GetValue(ShowAllAircraftProperty);
        set => SetValue(ShowAllAircraftProperty, value);
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
            ShowArtccProperty,
            ArtccBoundariesProperty,
            RadarImageDataProperty,
            ShowWeatherProperty,
            DisplaySettingsProperty,
            RadarOpacityProperty,
            RangeRingsProperty);
    }

    public TsdRadarControl()
    {
        Focusable = true;
        RebuildRenderCache();
    }

    // -------------------------------------------------------------------------
    // Lambert Conformal Conic projection
    // -------------------------------------------------------------------------

    private static (double x, double y) LccProject(
        double lat, double lon,
        double centerLat, double centerLon)
    {
        double phi = lat * Math.PI / 180.0;
        double lam = lon * Math.PI / 180.0;
        double phi0 = centerLat * Math.PI / 180.0;
        double lam0 = centerLon * Math.PI / 180.0;

        double n = Math.Log(Math.Cos(Phi1) / Math.Cos(Phi2)) /
                   Math.Log(Math.Tan(Math.PI / 4.0 + Phi2 / 2.0) /
                            Math.Tan(Math.PI / 4.0 + Phi1 / 2.0));

        double F = Math.Cos(Phi1) *
                   Math.Pow(Math.Tan(Math.PI / 4.0 + Phi1 / 2.0), n) / n;

        double rho = F / Math.Pow(Math.Tan(Math.PI / 4.0 + phi / 2.0), n);
        double rho0 = F / Math.Pow(Math.Tan(Math.PI / 4.0 + phi0 / 2.0), n);

        double theta = n * (lam - lam0);

        double x = rho * Math.Sin(theta);
        double y = rho0 - rho * Math.Cos(theta);

        return (x, y);
    }

    private static (double lat, double lon) LccInverse(
        double px, double py,
        double centerLat, double centerLon)
    {
        double phi0 = centerLat * Math.PI / 180.0;
        double lam0 = centerLon * Math.PI / 180.0;

        double n = Math.Log(Math.Cos(Phi1) / Math.Cos(Phi2)) /
                   Math.Log(Math.Tan(Math.PI / 4.0 + Phi2 / 2.0) /
                            Math.Tan(Math.PI / 4.0 + Phi1 / 2.0));

        double F = Math.Cos(Phi1) *
                   Math.Pow(Math.Tan(Math.PI / 4.0 + Phi1 / 2.0), n) / n;

        double rho0 = F / Math.Pow(Math.Tan(Math.PI / 4.0 + phi0 / 2.0), n);

        double rho = Math.Sign(n) * Math.Sqrt(
            px * px + (rho0 - py) * (rho0 - py));
        double theta = Math.Atan2(px, rho0 - py);

        double phi = 2.0 * Math.Atan(Math.Pow(F / rho, 1.0 / n))
                     - Math.PI / 2.0;
        double lam = lam0 + theta / n;

        return (phi * 180.0 / Math.PI, lam * 180.0 / Math.PI);
    }

    private Point LatLonToScreen(double lat, double lon,
                                  double width, double height)
    {
        double scale = Math.Min(width, height) * 0.45 * ZoomLevel;

        var (px, py) = LccProject(lat, lon, CenterLat, CenterLon);
        var (cx, cy) = LccProject(CenterLat, CenterLon, CenterLat, CenterLon);

        double x = width / 2.0 + (px - cx) * scale / (Math.PI / 180.0) / 57.0;
        double y = height / 2.0 - (py - cy) * scale / (Math.PI / 180.0) / 57.0;

        return new Point(x, y);
    }

    private bool IsOnScreen(Point pt, double width, double height)
    {
        return pt.X >= -20 && pt.X <= width + 20 &&
               pt.Y >= -20 && pt.Y <= height + 20;
    }

    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
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
            change.Property == ArtccBoundariesProperty ||
            change.Property == ShowArtccProperty ||
            change.Property == DisplaySettingsProperty ||
            change.Property == RangeRingsProperty)
        {
            _geometriesDirty = true;

            if (change.OldValue is INotifyCollectionChanged old)
                old.CollectionChanged -= OnCollectionChanged;
            if (change.NewValue is INotifyCollectionChanged neu)
                neu.CollectionChanged += OnCollectionChanged;

            InvalidateVisual();
        }

        if (change.Property == RadarImageDataProperty)
        {
            if (change.NewValue is byte[] data && data.Length > 0)
            {
                try
                {
                    using var ms = new System.IO.MemoryStream(data);
                    _radarBitmap?.Dispose();
                    _radarBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"TsdRadarControl: bitmap error — {ex.Message}");
                    _radarBitmap?.Dispose();
                    _radarBitmap = null;
                }
            }
            else
            {
                _radarBitmap?.Dispose();
                _radarBitmap = null;
            }
            InvalidateVisual();
        }

        if (change.Property == DisplaySettingsProperty)
        {
            RebuildRenderCache();
            InvalidateVisual();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _geometriesDirty = true;
        InvalidateVisual();
    }

    private void OnCollectionChanged(
        object? sender, NotifyCollectionChangedEventArgs e)
    {
        _geometriesDirty = true;
        InvalidateVisual();
    }

    private void RebuildGeometries(double width, double height)
    {
        _stateBoundaryGeometry = BuildPolylineGeometry(
            StateBoundaries.Select(b => b.Points).ToList(),
            width, height);

        _countryBoundaryGeometry = BuildPolylineGeometry(
            CountryBoundaries.Select(b => b.Points).ToList(),
            width, height);

        _sectorGeometry = BuildPolylineGeometry(
            Sectors.Select(s => s.Points).ToList(),
            width, height);

        if (ArtccBoundaries?.Count > 0)
        {
            var geo = new GeometryGroup();
            foreach (var artcc in ArtccBoundaries)
            {
                if (artcc.Points.Count < 2) continue;
                var path = new PathGeometry();
                PathFigure? figure = null;

                foreach (var point in artcc.Points)
                {
                    if (point is null) continue;
                    if (double.IsNaN(point.Lat) || double.IsNaN(point.Lon))
                    {
                        if (figure != null)
                        {
                            figure.IsClosed = true;
                            path.Figures!.Add(figure);
                            figure = null;
                        }
                        continue;
                    }

                    var screenPt = LatLonToScreen(
                        point.Lat, point.Lon, width, height);

                    if (figure == null)
                    {
                        figure = new PathFigure
                        {
                            StartPoint = screenPt,
                            IsClosed = false
                        };
                    }
                    else
                    {
                        figure.Segments!.Add(
                            new LineSegment { Point = screenPt });
                    }
                }

                if (figure != null)
                {
                    figure.IsClosed = true;
                    path.Figures!.Add(figure);
                }

                geo.Children.Add(path);
            }
            _artccBoundaryGeometry = geo;
        }
        else
        {
            _artccBoundaryGeometry = null;
        }

        _cachedCenterLat = CenterLat;
        _cachedCenterLon = CenterLon;
        _cachedZoom = ZoomLevel;
        _cachedWidth = width;
        _cachedHeight = height;
        _geometriesDirty = false;
    }

    private Geometry BuildPolylineGeometry(
        List<List<LatLon>> polylines,
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

    private void RebuildRenderCache()
    {
        var s = DisplaySettings;

        _backgroundBrush = new SolidColorBrush(
            Color.Parse(s.BackgroundColor));
        _boundaryPen = new Pen(
            new SolidColorBrush(Color.Parse(s.BoundaryColor)), 0.8);
        _sectorPen = new Pen(
            new SolidColorBrush(Color.Parse(s.TraconColor)), 1.0);
        _artccPen = new Pen(
            new SolidColorBrush(Color.Parse(s.ArtccColor)), 1.5);
        _jetRoutePen = new Pen(
            new SolidColorBrush(Color.Parse(s.JetRoutesColor)), 0.8);
        _victorRoutePen = new Pen(
            new SolidColorBrush(Color.Parse(s.VictorRoutesColor)), 0.8);
        _airportBrush = new SolidColorBrush(Color.Parse(s.AirportColor));
        _vorBrush = new SolidColorBrush(Color.Parse(s.VorColor));
        _vorPen = new Pen(_vorBrush, 1.0);
        _ndbBrush = new SolidColorBrush(Color.Parse(s.NdbColor));
        _ndbPen = new Pen(_ndbBrush, 1.0);
        _fixBrush = new SolidColorBrush(Color.Parse(s.FixColor));
        _fixPen = new Pen(_fixBrush, 0.8);
        _traconBrush = new SolidColorBrush(Color.Parse(s.TraconColor));
        _traconPen = new Pen(_traconBrush, 1.0);
        _dataBlockBrush = new SolidColorBrush(Color.Parse(s.DataBlockColor));
        _mapLabelBrush = new SolidColorBrush(Color.Parse(s.MapLabelColor));
        _dataBlockTypeface = new Typeface(s.DataBlockFont);
        _mapLabelTypeface = new Typeface(s.MapLabelFont);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        context.FillRectangle(
            _backgroundBrush, new Rect(0, 0, width, height));

        if (_geometriesDirty ||
            Math.Abs(_cachedWidth - width) > 0.5 ||
            Math.Abs(_cachedHeight - height) > 0.5)
        {
            RebuildGeometries(width, height);
        }

        if (ShowStateBoundaries && _stateBoundaryGeometry != null)
            context.DrawGeometry(
                null, _boundaryPen, _stateBoundaryGeometry);

        if (ShowCountryBoundaries && _countryBoundaryGeometry != null)
            context.DrawGeometry(
                null, _boundaryPen, _countryBoundaryGeometry);

        if (_sectorGeometry != null)
            context.DrawGeometry(null, _sectorPen, _sectorGeometry);

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

        if (ShowArtcc && _artccBoundaryGeometry != null)
            context.DrawGeometry(null, _artccPen, _artccBoundaryGeometry);

        DrawRangeRings(context, width, height);
        DrawRoutes(context, width, height);
        DrawActiveMapItems(context, width, height);
        DrawAircraft(context, width, height);
    }

    private void ScheduleRadarRefresh()
    {
        if (!ShowWeather) return;

        _radarRefreshTimer?.Dispose();
        _radarRefreshTimer = new System.Threading.Timer(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                RadarRefreshRequested?.Invoke(this, EventArgs.Empty));
        }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }

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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var width = Bounds.Width;
        var height = Bounds.Height;

        switch (e.Key)
        {
            case Key.M:
                // Save undo state
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

            case Key.Z:
                _prevCenterLat = CenterLat;
                _prevCenterLon = CenterLon;
                _prevZoomLevel = ZoomLevel;

                ZoomLevel = Math.Clamp(ZoomLevel * 1.25, 0.25, 20.0);
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

            case Key.W:
                ShowWeather = !ShowWeather;
                if (ShowWeather)
                    ScheduleRadarRefresh();
                e.Handled = true;
                break;

            case Key.F:
                ShowAllAircraft = !ShowAllAircraft;
                e.Handled = true;
                break;

            case Key.D:
                _geometriesDirty = true;
                InvalidateVisual();
                e.Handled = true;
                break;
        }
    }
    private void DrawAircraft(DrawingContext context,
                               double width, double height)
    {
        var typeface = _dataBlockTypeface;
        const double size = 4.0;

        foreach (var pilot in VisiblePilots)
        {
            var pt = LatLonToScreen(
                pilot.Lat, pilot.Lon, width, height);
            if (!IsOnScreen(pt, width, height)) continue;

            var brush = new SolidColorBrush(
                Color.Parse(pilot.MatchedFilterColor));

            DrawAircraftSymbol(context, pt, pilot.Heading, size, brush);

            if (_hoveredCallsign != null &&
                _hoveredCallsign == pilot.Callsign)
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
            $"{pilot.GroundSpeed}",
            pilot.Arrival
        };

        if (pilot.MatchedShowRoute &&
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
                _mapLabelTypeface, 9, _traconBrush);

            context.DrawText(label, new Point(
                center.X - label.Width / 2,
                center.Y - label.Height / 2));
        }
    }

    private void DrawRoutes(DrawingContext context,
        double width, double height)
    {
        foreach (var pilot in VisiblePilots)
        {
            if (!pilot.MatchedDrawRoute) continue;
            if (pilot.ParsedRoute is null ||
                pilot.ParsedRoute.Count < 2) continue;

            var brush = new SolidColorBrush(
                Color.Parse(pilot.MatchedFilterColor));
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
                    break;

                case string t when t.Contains("VOR"):
                    context.DrawEllipse(null, _vorPen, pt, 2.5, 4.0);

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
                    break;

                case string t when t.Contains("NDB"):
                    context.DrawEllipse(null, _ndbPen, pt, 2.5, 4.0);

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
                    break;

                case "TRACON":
                    DrawTraconBoundary(context, item, width, height);
                    break;

                case "Sector":
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

                    foreach (var labelRing in item.Rings)
                    {
                        if (labelRing.Count == 0) continue;

                        double sLat = labelRing.Average(p => p.Lat);
                        double sLon = labelRing.Average(p => p.Lon);
                        var center = LatLonToScreen(sLat, sLon, width, height);

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

                        double totalHeight = idLabel.Height + altLabel.Height + 2;

                        context.DrawText(idLabel, new Point(
                            center.X - idLabel.Width / 2,
                            center.Y - totalHeight / 2));

                        context.DrawText(altLabel, new Point(
                            center.X - altLabel.Width / 2,
                            center.Y - totalHeight / 2 + idLabel.Height + 2));
                    }
                    break;

                case "Airway":
                    if (item.Rings.Count > 0)
                    {
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

                        var midPt = item.Rings[0][item.Rings[0].Count / 2];
                        var midScreen = LatLonToScreen(
                            midPt.Lat, midPt.Lon, width, height);

                        if (IsOnScreen(midScreen, width, height))
                        {
                            var airwayLabel = new FormattedText(
                                item.Identifier,
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                _mapLabelTypeface, 9, _mapLabelBrush);

                            context.DrawText(airwayLabel, new Point(
                                midScreen.X - airwayLabel.Width / 2,
                                midScreen.Y - airwayLabel.Height / 2));
                        }
                    }
                    break;
            }
        }
    }
}