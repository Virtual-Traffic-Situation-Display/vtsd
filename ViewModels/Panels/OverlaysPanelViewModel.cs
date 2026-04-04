using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class OverlaysPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    public ObservableCollection<OverlayItem> Overlays { get; } = new();

    public OverlaysPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "OVERLAYS";
        _tsdViewModel = tsdViewModel;

        BuildOverlayItems();
        ReadFromTsd();
    }

    // =========================================================================
    // Build the overlay item list
    // =========================================================================

    private void BuildOverlayItems()
    {
        // Functional overlays (have backing properties on TsdViewModel)
        Overlays.Add(new OverlayItem
        {
            Name = "State Boundaries",
            PropertyKey = "ShowStateBoundaries"
        });
        Overlays.Add(new OverlayItem
        {
            Name = "Country Boundaries",
            PropertyKey = "ShowCountryBoundaries"
        });
        Overlays.Add(new OverlayItem
        {
            Name = "ARTCC Boundaries",
            PropertyKey = "ShowArtcc"
        });
        Overlays.Add(new OverlayItem
        {
            Name = "Airports",
            PropertyKey = "ShowAirports"
        });
        Overlays.Add(new OverlayItem
        {
            Name = "VORs",
            PropertyKey = "ShowVors"
        });
        Overlays.Add(new OverlayItem
        {
            Name = "NDBs",
            PropertyKey = "ShowNdbs"
        });
        Overlays.Add(new OverlayItem
        {
            Name = "Fixes / Waypoints",
            PropertyKey = "ShowWaypoints"
        });

        // Planned overlays (no backing property yet — disabled)
        Overlays.Add(new OverlayItem
        {
            Name = "Jet Airways",
            PropertyKey = null,
            IsEnabled = false
        });
        Overlays.Add(new OverlayItem
        {
            Name = "Victor Airways",
            PropertyKey = null,
            IsEnabled = false
        });
        Overlays.Add(new OverlayItem
        {
            Name = "TRACONs",
            PropertyKey = null,
            IsEnabled = false
        });
        Overlays.Add(new OverlayItem
        {
            Name = "Lat/Lon Grid",
            PropertyKey = null,
            IsEnabled = false
        });
        Overlays.Add(new OverlayItem
        {
            Name = "Sector Overlays",
            PropertyKey = null,
            IsEnabled = false
        });
        Overlays.Add(new OverlayItem
        {
            Name = "SUAs",
            PropertyKey = null,
            IsEnabled = false
        });
    }

    // =========================================================================
    // Read current overlay state from TsdViewModel
    // =========================================================================

    private void ReadFromTsd()
    {
        foreach (var item in Overlays)
        {
            if (item.PropertyKey == null) continue;

            bool isOn = item.PropertyKey switch
            {
                "ShowStateBoundaries"  => _tsdViewModel.ShowStateBoundaries,
                "ShowCountryBoundaries"=> _tsdViewModel.ShowCountryBoundaries,
                "ShowArtcc"            => _tsdViewModel.ShowArtcc,
                "ShowAirports"         => _tsdViewModel.ShowAirports,
                "ShowVors"             => _tsdViewModel.ShowVors,
                "ShowNdbs"             => _tsdViewModel.ShowNdbs,
                "ShowWaypoints"        => _tsdViewModel.ShowWaypoints,
                _ => false
            };

            item.State = isOn ? OverlayState.Show : OverlayState.Hide;
        }
    }

    // =========================================================================
    // Write overlay state back to TsdViewModel
    // =========================================================================

    public void ApplyToTsd()
    {
        foreach (var item in Overlays)
        {
            if (item.PropertyKey == null) continue;

            // Show = true, Hide = false
            bool isOn = item.State == OverlayState.Show;

            switch (item.PropertyKey)
            {
                case "ShowStateBoundaries":
                    _tsdViewModel.ShowStateBoundaries = isOn;
                    break;
                case "ShowCountryBoundaries":
                    _tsdViewModel.ShowCountryBoundaries = isOn;
                    break;
                case "ShowArtcc":
                    _tsdViewModel.ShowArtcc = isOn;
                    break;
                case "ShowAirports":
                    _tsdViewModel.ShowAirports = isOn;
                    break;
                case "ShowVors":
                    _tsdViewModel.ShowVors = isOn;
                    break;
                case "ShowNdbs":
                    _tsdViewModel.ShowNdbs = isOn;
                    break;
                case "ShowWaypoints":
                    _tsdViewModel.ShowWaypoints = isOn;
                    break;
            }
        }
    }

    // =========================================================================
    // Commands
    // =========================================================================

    [RelayCommand]
    private void ShowAll()
    {
        foreach (var item in Overlays)
        {
            if (!item.IsEnabled) continue;
            item.State = OverlayState.Show;
        }
    }

    [RelayCommand]
    private void HideAll()
    {
        foreach (var item in Overlays)
        {
            if (!item.IsEnabled) continue;
            item.State = OverlayState.Hide;
        }
    }

    [RelayCommand]
    private void Apply() => ApplyToTsd();

    [RelayCommand]
    private void Ok()
    {
        ApplyToTsd();
        OkRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => OkRequested?.Invoke(this, EventArgs.Empty);

    public event EventHandler? OkRequested;
}