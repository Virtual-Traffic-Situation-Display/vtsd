using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.Views;

public partial class TsdView : UserControl
{
    private TsdRadarControl? _radarControl;

    public TsdView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(
    Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _radarControl = this.FindControl<TsdRadarControl>("RadarControl");

        System.Diagnostics.Debug.WriteLine(
            $"TsdView: radar control found = {_radarControl != null}");

        if (_radarControl != null)
        {
            _radarControl.RadarRefreshRequested -= OnRadarRefreshRequested;
            _radarControl.RadarRefreshRequested += OnRadarRefreshRequested;
            _radarControl.RouteResolveRequested -= OnRouteResolveRequested;
            _radarControl.RouteResolveRequested += OnRouteResolveRequested;
            _radarControl.MapItemRemoveRequested -= OnMapItemRemoveRequested;
            _radarControl.MapItemRemoveRequested += OnMapItemRemoveRequested;
            _radarControl.GenericMenuCommandRequested -= OnGenericMenuCommand;
            _radarControl.GenericMenuCommandRequested += OnGenericMenuCommand;
            _radarControl.FlightDetailRequested -= OnFlightDetailRequested;
            _radarControl.FlightDetailRequested += OnFlightDetailRequested;
            System.Diagnostics.Debug.WriteLine(
                "TsdView: subscribed to RadarRefreshRequested");
        }
    }

    private void OnRadarRefreshRequested(object? sender, EventArgs e)
    {
        if (DataContext is TsdViewModel vm)
            vm.TriggerRadarRefresh();
    }

    private void OnRouteResolveRequested(object? sender, VatsimPilot pilot)
    {
        if (DataContext is TsdViewModel vm)
        {
            pilot.ParsedRoute = vm.ResolveRoute(pilot);
            _radarControl?.InvalidateVisual();
        }
    }
    private void OnMapItemRemoveRequested(object? sender, MapItem item)
    {
        if (DataContext is TsdViewModel vm)
            vm.RemoveMapItem(item);
    }

    private void OnGenericMenuCommand(object? sender, string command)
    {
        // Walk up to MainWindow to find MainViewModel
        var mainWindow = this.FindAncestorOfType<Window>();
        if (mainWindow?.DataContext is not MainViewModel mainVm) return;

        switch (command)
        {
            case "SelectFlights":
                mainVm.OpenSelectFlightsCommand.Execute(null);
                break;
            case "ShowMapItem":
                mainVm.OpenShowMapItemCommand.Execute(null);
                break;
            case "RangeRings":
                mainVm.OpenRangeRingsCommand.Execute(null);
                break;
            case "FindFlight":
                mainVm.OpenFindFlightCommand.Execute(null);
                break;
        }
    }

    private void OnFlightDetailRequested(object? sender, VatsimPilot pilot)
    {
        var mainWindow = this.FindAncestorOfType<Window>();
        if (mainWindow?.DataContext is not MainViewModel mainVm) return;

        mainVm.OpenFlightDetail(pilot);
    }
}
