using Avalonia.Controls;
using Avalonia.Input;
using System;
using vTFMS.Models;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class SelectFlightsPanelWindow : BasePanelWindow
{
    public SelectFlightsPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a SelectFlightsPanelViewModel.");
    }

    public SelectFlightsPanelWindow(SelectFlightsPanelViewModel vm)
    {
        vm.OkRequested += (_, _) => Close();
        DataContext = vm;
        InitializeComponent();
    }

    private void ColorSwatch_PointerPressed(object? sender,
        PointerPressedEventArgs e)
    {
        if (sender is Border border &&
            border.DataContext is FlightFilter filter)
        {
            var picker = new ColorPickerWindow(filter.Color);
            picker.ColorSelected += (_, hex) => filter.Color = hex;
            picker.Show(this);
        }
    }
}