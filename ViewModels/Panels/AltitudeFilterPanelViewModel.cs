using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class AltitudeFilterPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    [ObservableProperty]
    private bool _enabled;

    [ObservableProperty]
    private int _floor;

    [ObservableProperty]
    private int _ceiling;

    public AltitudeFilterPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Altitude Filter";
        _tsdViewModel = tsdViewModel;

        Enabled = _tsdViewModel.AltitudeFilterEnabled;
        Floor = _tsdViewModel.AltitudeFloor;
        Ceiling = _tsdViewModel.AltitudeCeiling;
    }

    [RelayCommand]
    private void Apply()
    {
        _tsdViewModel.AltitudeFilterEnabled = Enabled;
        _tsdViewModel.AltitudeFloor = Floor;
        _tsdViewModel.AltitudeCeiling = Ceiling;
    }

    [RelayCommand]
    private void Ok()
    {
        Apply();
        OkRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        OkRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OkRequested;
}