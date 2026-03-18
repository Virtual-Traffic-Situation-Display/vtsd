using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class SelectWeatherPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;


    [ObservableProperty]
    private string _statusMessage = "Weather is off";

    public bool ShowWeather
    {
        get => _tsdViewModel.ShowWeather;
        set
        {
            _tsdViewModel.ShowWeather = value;
            OnPropertyChanged();
            StatusMessage = value ? "Fetching radar..." : "Weather is off";
        }
    }

    public SelectWeatherPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "SELECT WEATHER";
        _tsdViewModel = tsdViewModel;
    }

    [RelayCommand]
    private async Task FetchWeather()
    {
        if (!ShowWeather)
        {
            StatusMessage = "Enable weather first";
            return;
        }

        StatusMessage = "Fetching radar...";

        try
        {
            double scale = 1.2;
            var (minLat, minLon, maxLat, maxLon) =
                _tsdViewModel.GetVisibleBounds(
                    _tsdViewModel.LastScreenWidth,
                    _tsdViewModel.LastScreenHeight);

            double latPad = (maxLat - minLat) * (scale - 1) / 2;
            double lonPad = (maxLon - minLon) * (scale - 1) / 2;

            int imgWidth = Math.Min(
                (int)(_tsdViewModel.LastScreenWidth * scale), 4096);
            int imgHeight = Math.Min(
                (int)(_tsdViewModel.LastScreenHeight * scale), 4096);

            await _tsdViewModel.RefreshRadarAsync(
                minLat - latPad,
                minLon - lonPad,
                maxLat + latPad,
                maxLon + lonPad,
                imgWidth, imgHeight);

            StatusMessage = _tsdViewModel.RadarImageData != null
                ? "Radar updated"
                : "Fetch failed — check connection";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}