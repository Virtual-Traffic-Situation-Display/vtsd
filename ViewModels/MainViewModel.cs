using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using vTFMS.Helpers;
using vTFMS.Models;
using vTFMS.Services;
using vTFMS.ViewModels;
using vTFMS.Views;
using vTFMS.Views.Panels;

namespace vTFMS.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IPanelWindowManager _panelManager;
    private readonly IProfileService _profileService;
    private readonly IWeatherService _weatherService;
    private readonly IMapDataService _mapDataService;
    private readonly IFontService _fontService;
    private readonly IUpdateService _updateService;
    private readonly Window _mainWindow;


    public bool VatsimConnected => TsdViewModel.VatsimConnected;

    public TsdViewModel TsdViewModel { get; }

    private FlightCountPanelWindow? _flightCountWindow;

    [RelayCommand]
    private void ToggleFlightCount()
    {
        if (_flightCountWindow != null)
        {
            _flightCountWindow.Close();
            _flightCountWindow = null;
            return;
        }

        _flightCountWindow = new FlightCountPanelWindow(TsdViewModel);
        _flightCountWindow.Closed += (_, _) =>
            _flightCountWindow = null;
        _flightCountWindow.Show(_mainWindow);
    }

    [RelayCommand]
    private void ToggleShowAllAircraft()
    {
        TsdViewModel.ShowAllAircraft = !TsdViewModel.ShowAllAircraft;
        OnPropertyChanged(nameof(ShowAllAircraft));
    }

    [RelayCommand]
    private void RestoreHiddenFlights()
    {
        foreach (var pilot in TsdViewModel.AllCurrentPilots)
            pilot.IsHidden = false;

        foreach (var pilot in TsdViewModel.VisiblePilots)
            pilot.IsHidden = false;
    }

    public bool ShowAllAircraft => TsdViewModel.ShowAllAircraft;

    [RelayCommand]
    private void ToggleVatsimData()
    {
        TsdViewModel.VatsimDataEnabled = !TsdViewModel.VatsimDataEnabled;
        OnPropertyChanged(nameof(VatsimDataEnabled));
    }

    public bool VatsimDataEnabled => TsdViewModel.VatsimDataEnabled;

    [ObservableProperty]
    private string _currentTime = string.Empty;

    public string AppVersion { get; } =
    Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "dev";

    private readonly System.Threading.Timer _clockTimer;



    public MainViewModel(IPanelWindowManager panelManager,
                         IMapDataService mapDataService,
                         IVatsimService vatsimService,
                         IProfileService profileService,
                         IWeatherService weatherService,
                         IFontService fontService,
                         IUpdateService updateService,
                         Window mainWindow)
    {
        _panelManager = panelManager;
        _profileService = profileService;
        _weatherService = weatherService;
        _fontService = fontService;
        _mapDataService = mapDataService;
        _updateService = updateService;
        _mainWindow = mainWindow;
        TsdViewModel = new TsdViewModel(mapDataService, vatsimService, weatherService);

        _clockTimer = new System.Threading.Timer(_ =>
        {
            CurrentTime = DateTime.UtcNow.ToString("HH:mm:ss") + "Z";
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        TsdViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TsdViewModel.VatsimConnected))
                OnPropertyChanged(nameof(VatsimConnected));
            if (e.PropertyName == nameof(TsdViewModel.VatsimDataEnabled))
                OnPropertyChanged(nameof(VatsimDataEnabled));
            if (e.PropertyName == nameof(TsdViewModel.ShowAllAircraft))
                OnPropertyChanged(nameof(ShowAllAircraft));
        };
    }

    // ── Panel commands ────────────────────────────────────────

    [RelayCommand]
    private void OpenSelectFeaFca() =>
        _panelManager.Open<SelectFeaFcaPanelWindow>();

    [RelayCommand]
    private void OpenMoveZoom() =>
        _panelManager.OpenWithArgs(
            new MoveZoomPanelWindow(TsdViewModel));

    [RelayCommand]
    private void OpenShowMapItem() =>
        _panelManager.OpenWithArgs(new ShowMapItemPanelWindow(TsdViewModel));

    [RelayCommand]
    private void OpenRangeRings() =>
        _panelManager.OpenWithArgs(
            new RangeRingsPanelWindow(TsdViewModel));

    [RelayCommand]
    private void OpenOverlays() =>
        _panelManager.OpenWithArgs(
            new OverlaysPanelWindow(TsdViewModel));

    [RelayCommand]
    private void OpenRunwayLayout() =>
        _panelManager.Open<RunwayLayoutPanelWindow>();

    [RelayCommand]
    private void OpenDme() =>
        _panelManager.Open<DmePanelWindow>();

    [RelayCommand]
    private void OpenProjection() =>
        _panelManager.Open<ProjectionPanelWindow>();

    [RelayCommand]
    private void OpenSelectFlights() =>
        _panelManager.OpenWithArgs(
            new SelectFlightsPanelWindow(
                TsdViewModel.SelectFlightsViewModel));

    [RelayCommand]
    private void OpenSelectWeather() =>
    _panelManager.OpenWithArgs(
        new SelectWeatherPanelWindow(TsdViewModel));

    [RelayCommand]
    private void OpenAdapt() =>
    _panelManager.OpenWithArgs(
        new AdaptPanelWindow(TsdViewModel, _fontService));

    [RelayCommand]
    private void OpenNasMonitor() =>
    _panelManager.OpenWithArgs(
        new NasMonitorPanelWindow(
            TsdViewModel, _mapDataService));

    [RelayCommand]
    private void OpenAbout()
    {
        var about = new AboutWindow(AppVersion);
        about.Show(_mainWindow);
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        var updateInfo = await _updateService.CheckForUpdatesAsync();
    
        if (updateInfo == null)
        {
            // No update found (or running in dev mode) — tell the user
            await MessageBoxHelper.ShowInfoAsync(
                _mainWindow,
                title: "No Updates",
                message: "You're already on the latest version.");
            return;
        }
    
        var newVersion = updateInfo.TargetFullRelease.Version;
    
        var confirmed = await MessageBoxHelper.ShowConfirmAsync(
            _mainWindow,
            title: "Update Available",
            message: $"Version {newVersion} is available. Download and install now?\n\nThe app will restart automatically.");
    
        if (!confirmed)
            return;
    
        await _updateService.DownloadAndApplyUpdateAsync(updateInfo);
        // Does not return — app restarts here
    }

    [RelayCommand]
    private void OpenAltitudeFilter() =>
        _panelManager.OpenWithArgs(
            new AltitudeFilterPanelWindow(TsdViewModel));

    [RelayCommand]
    private void Quit()
    {
        _mainWindow.Close();
    }

    // ── Filters commands ──────────────────────────────────────

    [RelayCommand]
    private async Task SaveFilters()
    {
        if (_profileService.LastFiltersPath != null)
            DoSaveFilters(_profileService.LastFiltersPath);
        else
            await SaveFiltersAs();
    }

    [RelayCommand]
    private async Task SaveFiltersAs()
    {
        var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save Filters",
                DefaultExtension = "json",
                SuggestedStartLocation = await _mainWindow.StorageProvider
                    .TryGetFolderFromPathAsync(
                        _profileService.FiltersDirectory),
                FileTypeChoices = new[]
                {
                new FilePickerFileType("Filter Profile")
                {
                    Patterns = new[] { "*.json" }
                }
                }
            });

        if (file != null)
            DoSaveFilters(file.Path.LocalPath);
    }

    private void DoSaveFilters(string path)
    {
        var profile = new FlightFilterProfile
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
            Filters = TsdViewModel.SelectFlightsViewModel.Filters.ToList(),
            CenterLat = TsdViewModel.CenterLat,
            CenterLon = TsdViewModel.CenterLon,
            ZoomLevel = TsdViewModel.ZoomLevel,
            ShowStateBoundaries = TsdViewModel.ShowStateBoundaries,
            ShowCountryBoundaries = TsdViewModel.ShowCountryBoundaries,
            ActiveMapItems = TsdViewModel.ActiveMapItems.ToList()
        };

        _profileService.SaveFilters(profile, path);
    }

    [RelayCommand]
    private async Task LoadFilters()
    {
        var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Load Filters",
                AllowMultiple = false,
                SuggestedStartLocation = await _mainWindow.StorageProvider
                    .TryGetFolderFromPathAsync(
                        _profileService.FiltersDirectory),
                FileTypeFilter = new[]
                {
                new FilePickerFileType("Filter Profile")
                {
                    Patterns = new[] { "*.json" }
                }
                }
            });

        if (files.Count > 0)
        {
            var profile = _profileService.LoadFilters(
                files[0].Path.LocalPath);

            TsdViewModel.SelectFlightsViewModel
                .LoadFilters(profile.Filters);
            TsdViewModel.SetFlightFilters(profile.Filters);

            TsdViewModel.CenterLat = profile.CenterLat;
            TsdViewModel.CenterLon = profile.CenterLon;
            TsdViewModel.ZoomLevel = profile.ZoomLevel;

            TsdViewModel.ShowStateBoundaries = profile.ShowStateBoundaries;
            TsdViewModel.ShowCountryBoundaries = profile.ShowCountryBoundaries;

            TsdViewModel.ActiveMapItems.Clear();
            foreach (var item in profile.ActiveMapItems)
                TsdViewModel.ActiveMapItems.Add(item);

            // Refresh weather for new map position
            TsdViewModel.TriggerRadarRefresh();
        }
    }

    // ── Profiles commands (stubbed for now) ───────────────────

    [RelayCommand]
    private void SaveProfile() { }

    [RelayCommand]
    private void SaveProfileAs() { }

    [RelayCommand]
    private void LoadProfile() { }

    public void Dispose()
    {
        _clockTimer.Dispose();
        TsdViewModel.Dispose();
    }
}