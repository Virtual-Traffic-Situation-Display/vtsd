using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class ColorPickerWindow : BasePanelWindow
{

    public ColorPickerWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a color string.");
    }

    public event EventHandler<string>? ColorSelected;

    public ColorPickerWindow(string currentColor = "#FFFFFF")
    {
        var vm = new BasePanelViewModel
        {
            Title = "SELECT COLOR"
        };
        DataContext = vm;
        InitializeComponent();
        BuildContent(currentColor);
    }

    private void BuildContent(string currentColor)
    {
        Avalonia.Media.Color initial;
        try { initial = Avalonia.Media.Color.Parse(currentColor); }
        catch { initial = Avalonia.Media.Colors.White; }

        // Preset colors
        var presets = new List<(string Name, string Hex)>
    {
        ("Black",   "#000000"),
        ("Blue",    "#0000FF"),
        ("Orange",  "#FF8C00"),
        ("Red",     "#FF0000"),
        ("Green",   "#008000"),
        ("Gray",    "#808080"),
        ("Yellow",  "#FFFF00"),
        ("Cyan",    "#00CCFF"),
        ("White",   "#FFFFFF"),
        ("Purple",  "#800080"),
        ("Magenta", "#FF00FF"),
        ("Lime",    "#00FF00"),
    };

        var presetWrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Avalonia.Thickness(8, 8, 8, 4)
        };

        var colorView = new Avalonia.Controls.ColorView
        {
            Color = initial,
            IsAlphaVisible = false,
            IsColorPaletteVisible = false,
            Margin = new Avalonia.Thickness(8, 0, 8, 0)
        };

        foreach (var (name, hex) in presets)
        {
            var swatch = new Border
            {
                Width = 28,
                Height = 22,
                Margin = new Avalonia.Thickness(2),
                CornerRadius = new Avalonia.CornerRadius(3),
                Background = new SolidColorBrush(
                    Avalonia.Media.Color.Parse(hex)),
                Cursor = new Avalonia.Input.Cursor(
                    Avalonia.Input.StandardCursorType.Hand),
                BorderThickness = new Avalonia.Thickness(2)
            };

            ToolTip.SetTip(swatch, name);

            // Clicking preset updates the color view
            swatch.PointerPressed += (_, _) =>
            {
                colorView.Color = Avalonia.Media.Color.Parse(hex);
                swatch.BorderBrush = null;
            };

            swatch.PointerEntered += (_, _) =>
                swatch.BorderBrush = new SolidColorBrush(
                    Avalonia.Media.Color.Parse("#FFFFFF"));
            swatch.PointerExited += (_, _) =>
                swatch.BorderBrush = null;

            presetWrap.Children.Add(swatch);
        }

        // Buttons
        var applyBtn = new Button
        {
            Content = "Apply",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Avalonia.Thickness(4)
        };

        var okBtn = new Button
        {
            Content = "OK",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Avalonia.Thickness(4)
        };

        var cancelBtn = new Button
        {
            Content = "Cancel",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Avalonia.Thickness(4)
        };

        applyBtn.Click += (_, _) =>
        {
            var hex = $"#{colorView.Color.R:X2}" +
                      $"{colorView.Color.G:X2}" +
                      $"{colorView.Color.B:X2}";
            ColorSelected?.Invoke(this, hex);
        };

        okBtn.Click += (_, _) =>
        {
            var hex = $"#{colorView.Color.R:X2}" +
                      $"{colorView.Color.G:X2}" +
                      $"{colorView.Color.B:X2}";
            ColorSelected?.Invoke(this, hex);
            Close();
        };

        cancelBtn.Click += (_, _) => Close();

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(8)
        };
        buttonRow.Children.Add(applyBtn);
        buttonRow.Children.Add(okBtn);
        buttonRow.Children.Add(cancelBtn);

        var mainStack = new StackPanel();
        mainStack.Children.Add(presetWrap);
        mainStack.Children.Add(colorView);
        mainStack.Children.Add(buttonRow);

        PanelContent.Content = mainStack;
    }
}