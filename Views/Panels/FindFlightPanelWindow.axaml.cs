using Avalonia.Controls;
using Avalonia.Media;
using System;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class FindFlightPanelWindow : BasePanelWindow
{
    private readonly FindFlightPanelViewModel _vm;

    public FindFlightPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public FindFlightPanelWindow(TsdViewModel tsdViewModel)
    {
        _vm = new FindFlightPanelViewModel(tsdViewModel);
        DataContext = _vm;
        InitializeComponent();

        _vm.OkRequested += (_, _) => Close();

        UpdateColorSwatch();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FindFlightPanelViewModel.HighlightColor))
                UpdateColorSwatch();
        };
    }

    private void UpdateColorSwatch()
    {
        var btn = this.FindControl<Button>("ColorButton");
        if (btn == null) return;

        try
        {
            btn.Background = new SolidColorBrush(
                Color.Parse(_vm.HighlightColor));
        }
        catch
        {
            btn.Background = new SolidColorBrush(Colors.Yellow);
        }
    }

    private void ColorButton_Click(object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var picker = new ColorPickerWindow(_vm.HighlightColor);
        picker.ColorSelected += (_, hex) =>
        {
            _vm.HighlightColor = hex;
        };
        picker.Show();
    }
}
