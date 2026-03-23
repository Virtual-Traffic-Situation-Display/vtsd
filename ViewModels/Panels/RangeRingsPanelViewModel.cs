using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class RangeRingsPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    [ObservableProperty]
    private string _centerIdentifier = string.Empty;

    [ObservableProperty]
    private int _intervalNm = 10;

    [ObservableProperty]
    private int _distanceNm = 40;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<RangeRingConfig> ActiveRings =>
        _tsdViewModel.RangeRings;

    public RangeRingsPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Range Rings";
        _tsdViewModel = tsdViewModel;
    }

    [RelayCommand]
    private void Add()
    {
        StatusMessage = string.Empty;

        var id = CenterIdentifier.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(id))
        {
            StatusMessage = "Enter an identifier";
            return;
        }

        if (IntervalNm <= 0)
        {
            StatusMessage = "Interval must be greater than 0";
            return;
        }

        if (DistanceNm <= 0)
        {
            StatusMessage = "Distance must be greater than 0";
            return;
        }

        if (DistanceNm % IntervalNm != 0)
        {
            StatusMessage = "Distance must be a multiple of interval";
            return;
        }

        // Check for duplicate
        if (ActiveRings.Any(r =>
            r.Identifier.Equals(id, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"{id} already has range rings";
            return;
        }

        // Resolve identifier to lat/lon
        var resolved = _tsdViewModel.ResolveIdentifier(id);
        if (resolved == null)
        {
            StatusMessage = $"{id} not found";
            return;
        }

        var (lat, lon) = resolved.Value;

        ActiveRings.Add(new RangeRingConfig
        {
            Identifier = id,
            CenterLat = lat,
            CenterLon = lon,
            IntervalNm = IntervalNm,
            DistanceNm = DistanceNm
        });

        StatusMessage = $"Added {id}: {DistanceNm}NM at {IntervalNm}NM intervals";
        CenterIdentifier = string.Empty;
    }

    [RelayCommand]
    private void Remove(RangeRingConfig config)
    {
        ActiveRings.Remove(config);
        StatusMessage = $"Removed {config.Identifier}";
    }

    [RelayCommand]
    private void ClearAll()
    {
        ActiveRings.Clear();
        StatusMessage = "All range rings cleared";
    }
}