using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class MoveZoomPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    [ObservableProperty]
    private double _latitude;

    [ObservableProperty]
    private double _longitude;

    [ObservableProperty]
    private double _rangeNm;

    [ObservableProperty]
    private string _centerIdentifier = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MoveZoomPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Move / Zoom";
        _tsdViewModel = tsdViewModel;

        // Load current values
        Latitude = _tsdViewModel.CenterLat;
        Longitude = _tsdViewModel.CenterLon;
        RangeNm = ZoomToNm(_tsdViewModel.ZoomLevel);
    }

    [RelayCommand]
    private void GoToIdentifier()
    {
        var id = CenterIdentifier.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(id))
        {
            StatusMessage = "Enter an identifier";
            return;
        }

        var resolved = _tsdViewModel.ResolveIdentifier(id);
        if (resolved == null)
        {
            StatusMessage = $"{id} not found";
            return;
        }

        var (lat, lon) = resolved.Value;
        Latitude = lat;
        Longitude = lon;
        StatusMessage = $"Centered on {id}";

        // Apply immediately when using Go
        _tsdViewModel.CenterLat = Latitude;
        _tsdViewModel.CenterLon = Longitude;
    }

    [RelayCommand]
    private void Apply()
    {
        if (RangeNm <= 0)
        {
            StatusMessage = "Range must be greater than 0";
            return;
        }

        _tsdViewModel.CenterLat = Latitude;
        _tsdViewModel.CenterLon = Longitude;
        _tsdViewModel.ZoomLevel = NmToZoom(RangeNm);
        StatusMessage = "Applied";
    }

    [RelayCommand]
    private void Reset()
    {
        Latitude = _tsdViewModel.CenterLat;
        Longitude = _tsdViewModel.CenterLon;
        RangeNm = ZoomToNm(_tsdViewModel.ZoomLevel);
        StatusMessage = string.Empty;
    }

    // Convert ZoomLevel to approximate visible range in NM
    // Based on: latRange = (screenH / 2 * 57.0) / (min(W,H) * 0.45 * zoom)
    // rangeNm = latRange * 60
    // We use a reference screen size for the conversion since
    // the panel doesn't know the actual radar control dimensions.
    // 800px is a reasonable reference (matches MinHeight).
    private static double ZoomToNm(double zoom)
    {
        const double refSize = 800.0;
        double scale = refSize * 0.45 * zoom;
        double latRange = refSize / 2 * 57.0 / scale;
        double nm = latRange * 60.0;
        return Math.Round(nm, 1);
    }

    private static double NmToZoom(double nm)
    {
        const double refSize = 800.0;
        double latRange = nm / 60.0;
        double scale = refSize / 2 * 57.0 / latRange;
        double zoom = scale / (refSize * 0.45);
        return Math.Clamp(zoom, 0.25, 20.0);
    }
}