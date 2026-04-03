using Avalonia;
using System.Collections.ObjectModel;
using vTFMS.Models;

namespace vTFMS.Views;

public partial class TsdRadarControl
{
    // =========================================================================
    // Styled Properties
    // =========================================================================

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

    public static readonly StyledProperty<FlightDisplaySettings>
        FlightDisplaySettingsProperty =
        AvaloniaProperty.Register<TsdRadarControl, FlightDisplaySettings>(
            nameof(FlightDisplaySettings), new FlightDisplaySettings());

    #endregion

    // =========================================================================
    // CLR Property Wrappers
    // =========================================================================

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

    public FlightDisplaySettings FlightDisplaySettings
    {
        get => GetValue(FlightDisplaySettingsProperty);
        set => SetValue(FlightDisplaySettingsProperty, value);
    }

    #endregion

    // =========================================================================
    // Static constructor — render invalidation
    // =========================================================================

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
            RangeRingsProperty,
            FlightDisplaySettingsProperty);
    }
}