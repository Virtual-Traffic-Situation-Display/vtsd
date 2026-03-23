using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using vTFMS.Models;
using vTFMS.Services;
using vTFMS.ViewModels.Panels;

namespace vTFMS.ViewModels;

public partial class TsdViewModel : ObservableObject, IDisposable
{
    private readonly IMapDataService _mapDataService;
    private readonly IVatsimService _vatsimService;
    private readonly IWeatherService _weatherService;
    private List<FlightFilter> _flightFilters = new();
    private readonly Dictionary<string, MapItem> _navDataCache = new();
    private List<TraconBoundary> _allTracons = new();

    public double LastScreenWidth => _lastScreenWidth;
    public double LastScreenHeight => _lastScreenHeight;

    [ObservableProperty]
    private double _centerLat = 39.5;

    [ObservableProperty]
    private double _centerLon = -98.35;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _showAirports = false;

    [ObservableProperty]
    private bool _showAllAircraft = false;

    partial void OnShowAllAircraftChanged(bool value)
    {
        RefreshVisiblePilots(_vatsimService.CurrentPilots);
    }

    [ObservableProperty]
    private bool _showWaypoints = false;

    [ObservableProperty]
    private bool _showVors = false;

    [ObservableProperty]
    private bool _showNdbs = false;

    [ObservableProperty]
    private bool _showStateBoundaries = true;

    [ObservableProperty]
    private bool _showCountryBoundaries = true;

    [ObservableProperty]
    private bool _showWeather = false;

    [ObservableProperty]
    private byte[]? _radarImageData;

    [ObservableProperty]
    private double _radarMinLat;

    [ObservableProperty]
    private double _radarMinLon;

    [ObservableProperty]
    private double _radarMaxLat;

    [ObservableProperty]
    private double _radarMaxLon;

    [ObservableProperty]
    private double _radarOpacity = 0.7;

    [ObservableProperty]
    private bool _vatsimConnected = false;

    private bool _vatsimDataEnabled;
    public bool VatsimDataEnabled
    {
        get => _vatsimDataEnabled;
        set
        {
            if (SetProperty(ref _vatsimDataEnabled, value))
            {
                if (value)
                {
                    _vatsimService.Start();
                }
                else
                {
                    _vatsimService.Stop();
                    VatsimConnected = false;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        VisiblePilots.Clear());
                }
            }
        }
    }

    [ObservableProperty]
    private bool _showArtcc = false;

    [ObservableProperty]
    private bool _showFlightCount = false;

    [ObservableProperty]
    private bool _altitudeFilterEnabled = false;

    [ObservableProperty]
    private int _altitudeFloor = 0;

    [ObservableProperty]
    private int _altitudeCeiling = 99999;

    partial void OnAltitudeFilterEnabledChanged(bool value)
    {
        RefreshVisiblePilots(_vatsimService.CurrentPilots);
    }

    partial void OnAltitudeFloorChanged(int value)
    {
        if (AltitudeFilterEnabled)
            RefreshVisiblePilots(_vatsimService.CurrentPilots);
    }

    partial void OnAltitudeCeilingChanged(int value)
    {
        if (AltitudeFilterEnabled)
            RefreshVisiblePilots(_vatsimService.CurrentPilots);
    }

    // Cached airway lists — populated on first request
    //private List<Airway>? _jetRoutes;
    //private List<Airway>? _victorRoutes;

    //public List<Airway> JetRoutes
    //{
    //    get
    //    {
    //        if (_jetRoutes == null)
    //        {
    //            _jetRoutes = _mapDataService.GetAllAirwaysByType("J");
    //            // Also include Q routes (high altitude RNAV)
    //            _jetRoutes.AddRange(_mapDataService.GetAllAirwaysByType("Q"));
    //        }
    //        return _jetRoutes;
    //    }
    //}

    //public List<Airway> VictorRoutes
    //{
    //    get
    //    {
    //        if (_victorRoutes == null)
    //        {
    //            _victorRoutes = _mapDataService.GetAllAirwaysByType("V");
    //            // Also include T routes (low altitude RNAV)
    //            _victorRoutes.AddRange(_mapDataService.GetAllAirwaysByType("T"));
    //        }
    //        return _victorRoutes;
    //    }
    //}

    public List<VatsimPilot> AllCurrentPilots =>
        _vatsimService.CurrentPilots;

    /// <summary>Raised after every VATSIM data refresh, before filtering.</summary>
    public event EventHandler? PilotsRefreshed;

    public ObservableCollection<ArtccBoundary> ArtccBoundaries { get; } = new();

    public DisplaySettings DisplaySettings { get; set; } = new();

    public void ApplyDisplaySettings()
    {
        OnPropertyChanged(nameof(DisplaySettings));
    }

    public async Task RefreshRadarAsync(
        double minLat, double minLon,
        double maxLat, double maxLon,
        int width, int height)
    {
        if (!ShowWeather) return;

        RadarMinLat = minLat;
        RadarMinLon = minLon;
        RadarMaxLat = maxLat;
        RadarMaxLon = maxLon;

        var data = await _weatherService.FetchRadarAsync(
            minLat, minLon, maxLat, maxLon, width, height);
        RadarImageData = data;
    }

    public async Task ResolveAllRoutesAsync(List<VatsimPilot> pilots)
    {
        await Task.Run(() =>
        {
            foreach (var pilot in pilots)
            {
                if (pilot.ParsedRoute.Count > 0) continue;
                if (string.IsNullOrWhiteSpace(pilot.Route))
                    continue;

                pilot.ParsedRoute = _mapDataService.ResolveRoute(
                    pilot.Departure,
                    pilot.Route,
                    pilot.Arrival);
            }
        });
    }

    public ObservableCollection<StateBoundary> StateBoundaries { get; } = new();
    public ObservableCollection<StateBoundary> CountryBoundaries { get; } = new();
    public ObservableCollection<SectorBoundary> Sectors { get; } = new();
    public ObservableCollection<MapItem> ActiveMapItems { get; } = new();
    public ObservableCollection<VatsimPilot> VisiblePilots { get; } = new();
    public SelectFlightsPanelViewModel SelectFlightsViewModel { get; }
    public ObservableCollection<RangeRingConfig> RangeRings { get; } = new();

    public TsdViewModel(IMapDataService mapDataService,
                        IVatsimService vatsimService,
                        IWeatherService weatherService)
    {
        _mapDataService = mapDataService;
        _vatsimService = vatsimService;
        _weatherService = weatherService;

        SelectFlightsViewModel = new SelectFlightsPanelViewModel(this);

        _vatsimService.PilotsUpdated += OnPilotsUpdated;

        _weatherService.AutoRefreshTriggered += OnAutoRefreshTriggered;

        LoadAllData();
    }

    public void RefreshRadarForCurrentView()
    {
        if (!ShowWeather) return;

        const double scale = 1.2;
        var (minLat, minLon, maxLat, maxLon) =
            GetVisibleBounds(_lastScreenWidth, _lastScreenHeight);

        double latPad = (maxLat - minLat) * (scale - 1) / 2;
        double lonPad = (maxLon - minLon) * (scale - 1) / 2;

        int imgWidth = Math.Min(
            (int)(_lastScreenWidth * scale), 4096);
        int imgHeight = Math.Min(
            (int)(_lastScreenHeight * scale), 4096);

        RefreshRadarAsync(
            minLat - latPad, minLon - lonPad,
            maxLat + latPad, maxLon + lonPad,
            imgWidth, imgHeight)
            .ContinueWith(t =>
                System.Diagnostics.Debug.WriteLine(
                    $"TsdViewModel: radar refresh failed — " +
                    $"{t.Exception?.GetBaseException().Message}"),
                System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
    }

    public void TriggerRadarRefresh()
    {
        System.Diagnostics.Debug.WriteLine(
            "TsdViewModel: TriggerRadarRefresh called");
        RefreshRadarForCurrentView();
    }

    private void OnAutoRefreshTriggered(object? sender, EventArgs e)
    {
        RefreshRadarForCurrentView();
    }

    private double _lastScreenWidth = 1920;
    private double _lastScreenHeight = 1080;

    public void UpdateScreenSize(double width, double height)
    {
        _lastScreenWidth = width;
        _lastScreenHeight = height;
    }

    private void LoadAllData()
    {
        foreach (var b in _mapDataService.LoadStateBoundaries())
            StateBoundaries.Add(b);

        foreach (var b in _mapDataService.LoadCountryBoundaries())
            CountryBoundaries.Add(b);

        _allTracons = _mapDataService.LoadTraconBoundaries();
        System.Diagnostics.Debug.WriteLine(
            $"TsdViewModel: loaded {_allTracons.Count} TRACONs");

        foreach (var b in _mapDataService.LoadArtccBoundaries())
            ArtccBoundaries.Add(b);
    }

    public (bool Found, string Message) TryAddMapItems(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, "No input provided");

        var identifiers = input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        int added = 0;
        int duplicate = 0;
        int notFound = 0;
        var notFoundList = new List<string>();

        foreach (var id in identifiers)
        {
            if (ActiveMapItems.Any(m => m.Identifier == id))
            {
                duplicate++;
                continue;
            }

            if (_navDataCache.TryGetValue(id, out var cached))
            {
                ActiveMapItems.Add(cached);
                added++;
                continue;
            }

            MapItem? item = null;

            var airport = _mapDataService.FindAirport(id);
            if (airport != null)
            {
                item = new MapItem
                {
                    Identifier = airport.Identifier,
                    Type = "Airport",
                    Lat = airport.Lat,
                    Lon = airport.Lon
                };
            }

            if (item == null)
            {
                var navaid = _mapDataService.FindNavaid(id);
                if (navaid != null)
                {
                    item = new MapItem
                    {
                        Identifier = navaid.Identifier,
                        Type = navaid.Type,
                        Lat = navaid.Lat,
                        Lon = navaid.Lon
                    };
                }
            }

            if (item == null)
            {
                var waypoint = _mapDataService.FindWaypoint(id);
                if (waypoint != null)
                {
                    item = new MapItem
                    {
                        Identifier = waypoint.Identifier,
                        Type = "Fix",
                        Lat = waypoint.Lat,
                        Lon = waypoint.Lon
                    };
                }
            }

            if (item == null)
            {
                var tracons = _allTracons
                    .Where(t => t.Identifier
                        .Equals(id, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (tracons.Any())
                {
                    var allRings = tracons
                        .SelectMany(t => t.Rings)
                        .ToList();

                    var firstRing = allRings.FirstOrDefault();
                    double lat = firstRing?.Average(p => p.Lat) ?? 0;
                    double lon = firstRing?.Average(p => p.Lon) ?? 0;

                    item = new MapItem
                    {
                        Identifier = id,
                        Type = "TRACON",
                        Lat = lat,
                        Lon = lon,
                        Rings = allRings
                    };
                }
            }

            if (item == null)
            {
                var airway = _mapDataService.GetAirway(id);
                System.Diagnostics.Debug.WriteLine(
        $"TryAddMapItems: airway lookup for '{id}' = " +
        $"{(airway == null ? "null" : $"{airway.Identifier} ({airway.WaypointNames.Count} waypoints)")}");
                if (airway != null && airway.ResolvedPoints.Count > 0)
                {
                    var points = airway.ResolvedPoints
                        .Where(p => p != null)
                        .Select(p => p!)
                        .ToList();

                    if (points.Count > 0)
                    {
                        double avgLat = points.Average(p => p.Lat);
                        double avgLon = points.Average(p => p.Lon);

                        item = new MapItem
                        {
                            Identifier = id,
                            Type = "Airway",
                            Lat = avgLat,
                            Lon = avgLon,
                            Rings = new List<List<LatLon>> { points }
                        };
                    }
                }
            }

            if (item == null)
            {
                var sectors = _mapDataService.GetSectors(id);
                if (sectors.Count > 0)
                {
                    var allPoints = sectors
                        .SelectMany(s => s.Rings)
                        .SelectMany(r => r)
                        .ToList();
                    double avgLat = allPoints.Average(p => p.Lat);
                    double avgLon = allPoints.Average(p => p.Lon);

                    // Use first sector for label/alt info
                    var first = sectors[0];

                    item = new MapItem
                    {
                        Identifier = first.Label,
                        Type = "Sector",
                        Lat = avgLat,
                        Lon = avgLon,
                        Rings = sectors.SelectMany(s => s.Rings).ToList(),
                        Label = first.AltLabel
                    };
                }
            }

            if (item != null)
            {
                _navDataCache[id] = item;
                ActiveMapItems.Add(item);
                added++;
            }
            else
            {
                notFound++;
                notFoundList.Add(id);
            }
        }

        var message = $"Added {added}";
        if (duplicate > 0) message += $", {duplicate} already in list";
        if (notFound > 0) message += $", not found: {string.Join(" ", notFoundList)}";

        return (added > 0, message);
    }

    public (double lat, double lon)? ResolveIdentifier(string id)
    {
        var airport = _mapDataService.FindAirport(id);
        if (airport != null) return (airport.Lat, airport.Lon);
    
        var navaid = _mapDataService.FindNavaid(id);
        if (navaid != null) return (navaid.Lat, navaid.Lon);
    
        var waypoint = _mapDataService.FindWaypoint(id);
        if (waypoint != null) return (waypoint.Lat, waypoint.Lon);
    
        return null;
    }

    public void RemoveMapItem(MapItem item)
    {
        ActiveMapItems.Remove(item);
    }

    public void SetFlightFilters(List<FlightFilter> filters)
    {
        _flightFilters = filters;
        RefreshVisiblePilots(_vatsimService.CurrentPilots);
    }

    private void OnPilotsUpdated(object? sender, List<VatsimPilot> pilots)
    {
        VatsimConnected = pilots.Count > 0;
        PilotsRefreshed?.Invoke(this, EventArgs.Empty);
        RefreshVisiblePilots(pilots);
    }

    private void RefreshVisiblePilots(List<VatsimPilot> pilots)
    {
        Task.Run(() =>
        {
            var activeFilters = _flightFilters
                .Where(f => f.Show &&
                    (!string.IsNullOrWhiteSpace(f.Arrival) ||
                     !string.IsNullOrWhiteSpace(f.Departure)))
                .ToList();
    
            if (!ShowAllAircraft && !activeFilters.Any())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    VisiblePilots.Clear());
                return;
            }
    
            var matched = new List<VatsimPilot>();
    
            foreach (var pilot in pilots)
            {
                if (!IsInMapView(pilot.Lat, pilot.Lon)) continue;

                // Altitude filter
                if (AltitudeFilterEnabled &&
                    (pilot.Altitude < AltitudeFloor ||
                     pilot.Altitude > AltitudeCeiling))
                    continue;
    
                bool matchedFilter = false;
                foreach (var filter in activeFilters)
                {
                    bool arrivalMatch =
                        string.IsNullOrWhiteSpace(filter.Arrival) ||
                        filter.Arrival
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Any(a => pilot.Arrival.Contains(
                                a.ToUpperInvariant(),
                                StringComparison.OrdinalIgnoreCase));
    
                    bool departureMatch =
                        string.IsNullOrWhiteSpace(filter.Departure) ||
                        filter.Departure
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Any(d => pilot.Departure.Contains(
                                d.ToUpperInvariant(),
                                StringComparison.OrdinalIgnoreCase));
    
                    if (arrivalMatch && departureMatch)
                    {
                        matchedFilter = true;
                        pilot.MatchedFilterColor = filter.Color;
                        pilot.MatchedDrawRoute = filter.DrawRoute;
                        pilot.MatchedShowRoute = filter.ShowRoute;
                        break;
                    }
                }
    
                if (matchedFilter)
                {
                    if (pilot.MatchedDrawRoute &&
                        pilot.ParsedRoute.Count == 0 &&
                        !string.IsNullOrWhiteSpace(pilot.Route))
                    {
                        pilot.ParsedRoute = _mapDataService.ResolveRoute(
                            pilot.Departure, pilot.Route, pilot.Arrival);
                    }
                    matched.Add(pilot);
                }
                else if (ShowAllAircraft)
                {
                    // Unfiltered aircraft get default appearance
                    pilot.MatchedFilterColor = "#FFFFFF";
                    pilot.MatchedDrawRoute = false;
                    pilot.MatchedShowRoute = false;
                    matched.Add(pilot);
                }
            }
    
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                VisiblePilots.Clear();
                foreach (var pilot in matched)
                    VisiblePilots.Add(pilot);
            });
        });
    }
    private void OnRadarUpdated(object? sender, byte[]? data)
    {
        if (data == null) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RadarImageData = data;
        });
    }

    private bool IsInMapView(double lat, double lon)
    {
        double range = 50.0 / ZoomLevel;
        return Math.Abs(lat - CenterLat) < range &&
               Math.Abs(lon - CenterLon) < range * 1.5;
    }

    public (double minLat, double minLon, double maxLat, double maxLon)
    GetVisibleBounds(double screenWidth, double screenHeight)
    {
        double scale = Math.Min(screenWidth, screenHeight)
            * 0.45 * ZoomLevel;

        double latRange = screenHeight / 2 * 57.0 / scale;
        double lonRange = screenWidth / 2 * 57.0 / scale;

        return (
            CenterLat - latRange,
            CenterLon - lonRange,
            CenterLat + latRange,
            CenterLon + lonRange
        );
    }

    public void Dispose()
    {
        _vatsimService.PilotsUpdated -= OnPilotsUpdated;
        _weatherService.AutoRefreshTriggered -= OnAutoRefreshTriggered;
        _vatsimService.Stop();
        _weatherService.Stop();
    }
}