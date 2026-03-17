using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
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
            System.Diagnostics.Debug.WriteLine(
                "TsdView: subscribed to RadarRefreshRequested");
        }
    }

    private void OnRadarRefreshRequested(object? sender, EventArgs e)
    {
        if (DataContext is TsdViewModel vm)
            vm.TriggerRadarRefresh();
    }
}