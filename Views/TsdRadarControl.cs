using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using vTFMS.Models;

namespace vTFMS.Views;

/// <summary>
/// Custom radar display control that renders aviation map overlays,
/// weather radar, VATSIM flight data, and interactive map items.
///
/// Split across partial class files for maintainability:
///   TsdRadarControl.cs             — Core state, events, geometry/cache management
///   TsdRadarControl.Properties.cs  — Avalonia StyledProperties and CLR wrappers
///   TsdRadarControl.Projection.cs  — Lambert Conformal Conic projection math
///   TsdRadarControl.Input.cs       — Pointer, context menu, and keyboard handling
///   TsdRadarControl.Render.cs      — Render() and all Draw* methods
/// </summary>
public partial class TsdRadarControl : Control
{
    // =========================================================================
    // Cached geometry state
    // =========================================================================

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

    // =========================================================================
    // Interaction state
    // =========================================================================

    private Point _currentMousePosition;
    private VatsimPilot? _hoveredPilot;
    private string? _hoveredCallsign;

    // Undo state for move/zoom
    private double _prevCenterLat;
    private double _prevCenterLon;
    private double _prevZoomLevel;

    // =========================================================================
    // Timers
    // =========================================================================

    private Timer? _radarRefreshTimer;

    // =========================================================================
    // Cached render objects
    // =========================================================================

    private Avalonia.Media.Imaging.Bitmap? _radarBitmap;

    // Brushes and pens — rebuilt when DisplaySettings changes
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

    // =========================================================================
    // Events
    // =========================================================================

    /// <summary>Raised after a move/zoom to trigger weather radar refresh.</summary>
    public event EventHandler? RadarRefreshRequested;

    /// <summary>Raised when a flight's route needs on-demand resolution.</summary>
    public event EventHandler<VatsimPilot>? RouteResolveRequested;

    /// <summary>Raised when a map item should be removed via context menu.</summary>
    public event EventHandler<MapItem>? MapItemRemoveRequested;

    /// <summary>Raised when the generic context menu requests a panel command
    /// (e.g. "SelectFlights", "ShowMapItem", "RangeRings").</summary>
    public event EventHandler<string>? GenericMenuCommandRequested;

    /// <summary>Raised when a flight icon is left-clicked to open detail view.</summary>
    public event EventHandler<VatsimPilot>? FlightDetailRequested;

    // =========================================================================
    // Constructor
    // =========================================================================

    public TsdRadarControl()
    {
        Focusable = true;
        RebuildRenderCache();
    }

    // =========================================================================
    // Property change handling
    // =========================================================================

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
            change.Property == RangeRingsProperty ||
            change.Property == FlightDisplaySettingsProperty)
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

    // =========================================================================
    // Weather radar refresh scheduling
    // =========================================================================

    private void ScheduleRadarRefresh()
    {
        if (!ShowWeather) return;

        _radarRefreshTimer?.Dispose();
        _radarRefreshTimer = new Timer(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                RadarRefreshRequested?.Invoke(this, EventArgs.Empty));
        }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }

    // =========================================================================
    // Render cache — rebuilds brushes/pens from DisplaySettings
    // =========================================================================

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

    // =========================================================================
    // Geometry building
    // =========================================================================

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
}
