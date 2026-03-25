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
}