using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class SelectWeatherPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    private double _screenWidth = 1920;
    private double _screenHeight = 1080;

    public void SetScreenSize(double width, double height)
    {
        _screenWidth = width;
        _screenHeight = height;
    }

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
            await Task.Run(() =>
                _tsdViewModel.RefreshRadarForCurrentView());

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