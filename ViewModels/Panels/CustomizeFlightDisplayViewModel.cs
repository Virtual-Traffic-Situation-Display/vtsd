using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class CustomizeFlightDisplayViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    // Show section
    [ObservableProperty]
    private bool _showDataBlocks;

    [ObservableProperty]
    private bool _showOrgDest;

    [ObservableProperty]
    private bool _showRouteText;

    // Draw section
    [ObservableProperty]
    private bool _drawRoutes;

    [ObservableProperty]
    private bool _showLastTz;

    [ObservableProperty]
    private bool _showLeadLines;

    [ObservableProperty]
    private int _leadLineMinutes = 5;

    // History section
    [ObservableProperty]
    private bool _collectHistory;

    [ObservableProperty]
    private int _historyIntervalMinutes = 5;

    [ObservableProperty]
    private bool _drawHistory;

    public CustomizeFlightDisplayViewModel(TsdViewModel tsdViewModel)
    {
        Title = "CUSTOMIZE FLIGHT DISPLAY";
        _tsdViewModel = tsdViewModel;
        ReadFromTsd();
    }

    // =========================================================================
    // Read current settings from TsdViewModel
    // =========================================================================

    private void ReadFromTsd()
    {
        var s = _tsdViewModel.FlightDisplaySettings;
        ShowDataBlocks = s.ShowDataBlocks;
        ShowOrgDest = s.ShowOrgDest;
        ShowRouteText = s.ShowRouteText;
        DrawRoutes = s.DrawRoutes;
        ShowLastTz = s.ShowLastTz;
        ShowLeadLines = s.ShowLeadLines;
        LeadLineMinutes = s.LeadLineMinutes;
        CollectHistory = s.CollectHistory;
        HistoryIntervalMinutes = s.HistoryIntervalMinutes;
        DrawHistory = s.DrawHistory;
    }

    // =========================================================================
    // Write settings back to TsdViewModel
    // =========================================================================

    private void ApplyToTsd()
    {
        var s = _tsdViewModel.FlightDisplaySettings;
        s.ShowDataBlocks = ShowDataBlocks;
        s.ShowOrgDest = ShowOrgDest;
        s.ShowRouteText = ShowRouteText;
        s.DrawRoutes = DrawRoutes;
        s.ShowLastTz = ShowLastTz;
        s.ShowLeadLines = ShowLeadLines;
        s.LeadLineMinutes = LeadLineMinutes;
        s.CollectHistory = CollectHistory;
        s.HistoryIntervalMinutes = HistoryIntervalMinutes;
        s.DrawHistory = DrawHistory;

        _tsdViewModel.ApplyFlightDisplaySettings();
    }

    // =========================================================================
    // Commands
    // =========================================================================

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