using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class ColorPickerWindow : BasePanelWindow
{
    public event EventHandler<string>? ColorSelected;

    public ColorPickerWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a color string.");
    }

    public ColorPickerWindow(string currentColor = "#FFFFFF")
    {
        var hex = currentColor?.Trim() ?? "#FFFFFF";
        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        var vm = new BasePanelViewModel
        {
            Title = "SELECT COLOR"
        };
        DataContext = vm;
        InitializeComponent();
        BuildContent(hex);
    }

    private void BuildContent(string currentColor)
    {
        Avalonia.Media.Color initial;
        try { initial = Avalonia.Media.Color.Parse(currentColor); }
        catch { initial = Avalonia.Media.Colors.White; }

        var presets = new List<(string Name, string Hex)>
        {
            ("Red",     "#FF0000"),
            ("Orange",  "#FF8C00"),
            ("Yellow",  "#FFFF00"),
            ("Lime",    "#00FF00"),
            ("Green",   "#008000"),
            ("Cyan",    "#00CCFF"),
            ("Blue",    "#0000FF"),
            ("Purple",  "#800080"),
            ("Magenta", "#FF00FF"),
            ("White",   "#FFFFFF"),
            ("Gray",    "#808080"),
            ("Black",   "#000000"),
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

            swatch.PointerPressed += (_, _) =>
            {
                ColorSelected?.Invoke(this, hex);
                Close();
            };

            swatch.PointerEntered += (_, _) =>
                swatch.BorderBrush = new SolidColorBrush(
                    Avalonia.Media.Color.Parse("#FFFFFF"));
            swatch.PointerExited += (_, _) =>
                swatch.BorderBrush = null;

            presetWrap.Children.Add(swatch);
        }

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

        okBtn.Click += (_, _) =>
        {
            var selected = $"#{colorView.Color.R:X2}" +
                           $"{colorView.Color.G:X2}" +
                           $"{colorView.Color.B:X2}";
            ColorSelected?.Invoke(this, selected);
            Close();
        };

        cancelBtn.Click += (_, _) => Close();

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(8)
        };

        buttonRow.Children.Add(okBtn);
        buttonRow.Children.Add(cancelBtn);

        var mainStack = new StackPanel();
        mainStack.Children.Add(presetWrap);
        mainStack.Children.Add(colorView);
        mainStack.Children.Add(buttonRow);

        PanelBody = mainStack;
    }
}