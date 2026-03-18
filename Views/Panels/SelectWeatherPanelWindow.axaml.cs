using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;
using System;

namespace vTFMS.Views.Panels;

public partial class SelectWeatherPanelWindow : BasePanelWindow
{
    public SelectWeatherPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    private readonly TsdViewModel _tsdViewModel;

    public SelectWeatherPanelWindow(TsdViewModel tsdViewModel)
    {
        _tsdViewModel = tsdViewModel;

        var vm = new SelectWeatherPanelViewModel(tsdViewModel);
        DataContext = vm;
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        var vm = (SelectWeatherPanelViewModel)DataContext!;

        var showWeatherCheck = new CheckBox
        {
            Content = "Show MRMS Radar",
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            Margin = new Avalonia.Thickness(8)
        };
        showWeatherCheck.Bind(CheckBox.IsCheckedProperty,
            new Avalonia.Data.Binding(nameof(vm.ShowWeather))
            { Mode = Avalonia.Data.BindingMode.TwoWay });

        var fetchButton = new Button
        {
            Content = "Refresh Now",
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            Margin = new Avalonia.Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        fetchButton.Bind(Button.CommandProperty,
            new Avalonia.Data.Binding(nameof(vm.FetchWeatherCommand)));

        var statusLabel = new TextBlock
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Margin = new Avalonia.Thickness(8, 0, 8, 8),
            Foreground = new SolidColorBrush(
                Avalonia.Media.Color.Parse("#000000"))
        };
        statusLabel.Bind(TextBlock.TextProperty,
            new Avalonia.Data.Binding(nameof(vm.StatusMessage)));

        var opacityLabel = new TextBlock
        {
            Text = "Opacity:",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(8, 0, 4, 0)
        };

        var opacitySlider = new Slider
        {
            Minimum = 0.1,
            Maximum = 1.0,
            Value = 0.7,
            Width = 120,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 4, 0)
        };
        opacitySlider.Bind(Slider.ValueProperty,
            new Avalonia.Data.Binding("RadarOpacity")
            {
                Source = _tsdViewModel,
                Mode = Avalonia.Data.BindingMode.TwoWay
            });

        var opacityValue = new TextBlock
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Width = 35,
            VerticalAlignment = VerticalAlignment.Center
        };
        opacitySlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
                opacityValue.Text = $"{opacitySlider.Value:P0}";
        };
        opacityValue.Text = $"{opacitySlider.Value:P0}";

        var opacityRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };
        opacityRow.Children.Add(opacityLabel);
        opacityRow.Children.Add(opacitySlider);
        opacityRow.Children.Add(opacityValue);

        // Set screen size on both ViewModel and TsdViewModel
        var screen = Screens.Primary;
        if (screen != null)
        {
            vm.SetScreenSize(
                screen.Bounds.Width,
                screen.Bounds.Height);
            _tsdViewModel.UpdateScreenSize(
                screen.Bounds.Width,
                screen.Bounds.Height);
        }

        var stack = new StackPanel { Margin = new Avalonia.Thickness(4) };
        stack.Children.Add(showWeatherCheck);
        stack.Children.Add(fetchButton);
        stack.Children.Add(statusLabel);
        stack.Children.Add(opacityRow);

        PanelContent.Content = stack;
    }
}