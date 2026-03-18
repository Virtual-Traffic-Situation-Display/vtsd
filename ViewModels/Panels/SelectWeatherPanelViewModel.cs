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

    public double RadarOpacity
    {
        get => _tsdViewModel.RadarOpacity;
        set
        {
            _tsdViewModel.RadarOpacity = value;
            OnPropertyChanged();
        }
    }

    public SelectWeatherPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Select Weather";
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