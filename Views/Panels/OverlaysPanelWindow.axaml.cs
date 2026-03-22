using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;
using System;

namespace vTFMS.Views.Panels;

public partial class OverlaysPanelWindow : BasePanelWindow
{
    private readonly TsdViewModel _tsdViewModel;

    public OverlaysPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public OverlaysPanelWindow(TsdViewModel tsdViewModel)
    {
        _tsdViewModel = tsdViewModel;

        var vm = new BasePanelViewModel { Title = "OVERLAYS" };
        DataContext = vm;
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        var stack = new StackPanel
        {
            Margin = new Avalonia.Thickness(4)
        };

        stack.Children.Add(MakeCheckBox("Show ARTCC Boundaries", "ShowArtcc"));
        //stack.Children.Add(MakeCheckBox("Show Jet Routes (J/Q)", "ShowJetRoutes"));
        //stack.Children.Add(MakeCheckBox("Show Victor Routes (V/T)", "ShowVictorRoutes"));
        //stack.Children.Add(MakeCheckBox("Show Airway Labels", "ShowAirwayLabels"));

        PanelBody = stack;
    }

    private CheckBox MakeCheckBox(string label, string bindingPath)
    {
        var cb = new CheckBox
        {
            Content = label,
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            Margin = new Avalonia.Thickness(8)
        };

        cb.Bind(CheckBox.IsCheckedProperty,
            new Avalonia.Data.Binding(bindingPath)
            {
                Source = _tsdViewModel,
                Mode = Avalonia.Data.BindingMode.TwoWay
            });

        return cb;
    }
}