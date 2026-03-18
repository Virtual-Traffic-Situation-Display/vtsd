using Avalonia.Controls;
using Avalonia.Input;
using System;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class AdaptPanelWindow : BasePanelWindow
{
    public AdaptPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public AdaptPanelWindow(TsdViewModel tsdViewModel)
    {
        var vm = new AdaptPanelViewModel(tsdViewModel);
        vm.OkRequested += (_, _) => Close();
        DataContext = vm;
        InitializeComponent();
    }

    private void ColorButton_PointerPressed(object? sender,
        PointerPressedEventArgs e)
    {
        if (sender is not Border border ||
            border.Tag is not string propName ||
            DataContext is not AdaptPanelViewModel vm)
            return;

        var currentHex = GetColor(vm, propName);
        var picker = new ColorPickerWindow(currentHex);
        picker.ColorSelected += (_, hex) => SetColor(vm, propName, hex);
        picker.Show(this);
    }

    private static string GetColor(AdaptPanelViewModel vm, string prop) =>
        prop switch
        {
            nameof(vm.BackgroundColor)   => vm.BackgroundColor,
            nameof(vm.BoundaryColor)     => vm.BoundaryColor,
            nameof(vm.TraconColor)       => vm.TraconColor,
            nameof(vm.ArtccColor)        => vm.ArtccColor,
            nameof(vm.AirportColor)      => vm.AirportColor,
            nameof(vm.VorColor)          => vm.VorColor,
            nameof(vm.NdbColor)          => vm.NdbColor,
            nameof(vm.FixColor)          => vm.FixColor,
            nameof(vm.JetRoutesColor)    => vm.JetRoutesColor,
            nameof(vm.VictorRoutesColor) => vm.VictorRoutesColor,
            nameof(vm.DataBlockColor)    => vm.DataBlockColor,
            nameof(vm.MapLabelColor)     => vm.MapLabelColor,
            _ => "#FFFFFF"
        };

    private static void SetColor(AdaptPanelViewModel vm,
        string prop, string hex)
    {
        switch (prop)
        {
            case nameof(vm.BackgroundColor):   vm.BackgroundColor = hex; break;
            case nameof(vm.BoundaryColor):     vm.BoundaryColor = hex; break;
            case nameof(vm.TraconColor):       vm.TraconColor = hex; break;
            case nameof(vm.ArtccColor):        vm.ArtccColor = hex; break;
            case nameof(vm.AirportColor):      vm.AirportColor = hex; break;
            case nameof(vm.VorColor):          vm.VorColor = hex; break;
            case nameof(vm.NdbColor):          vm.NdbColor = hex; break;
            case nameof(vm.FixColor):          vm.FixColor = hex; break;
            case nameof(vm.JetRoutesColor):    vm.JetRoutesColor = hex; break;
            case nameof(vm.VictorRoutesColor): vm.VictorRoutesColor = hex; break;
            case nameof(vm.DataBlockColor):    vm.DataBlockColor = hex; break;
            case nameof(vm.MapLabelColor):     vm.MapLabelColor = hex; break;
        }
    }
}