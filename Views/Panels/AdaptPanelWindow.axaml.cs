using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class AdaptPanelWindow : BasePanelWindow
{
    private AdaptPanelViewModel _vm;

    public AdaptPanelWindow(TsdViewModel tsdViewModel)
    {
        _vm = new AdaptPanelViewModel(tsdViewModel);
        _vm.OkRequested += (_, _) => Close();
        DataContext = _vm;
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        // Define all color items
        var items = new List<(string Label, string PropName,
            bool HasFont, string FontProp, string FontSizeProp)>
        {
            ("Background",          nameof(_vm.BackgroundColor),
                false, "", ""),
            ("State/Country",       nameof(_vm.BoundaryColor),
                false, "", ""),
            ("TRACON",              nameof(_vm.TraconColor),
                false, "", ""),
            ("ARTCC",               nameof(_vm.ArtccColor),
                false, "", ""),
            ("All Airports",        nameof(_vm.AirportColor),
                false, "", ""),
            ("VOR",                 nameof(_vm.VorColor),
                false, "", ""),
            ("NDB",                 nameof(_vm.NdbColor),
                false, "", ""),
            ("Departure Fix",       nameof(_vm.FixColor),
                false, "", ""),
            ("Jet Routes",          nameof(_vm.JetRoutesColor),
                false, "", ""),
            ("Victor Routes",       nameof(_vm.VictorRoutesColor),
                false, "", ""),
            ("Flight DataBlock",    nameof(_vm.DataBlockColor),
                true,
                nameof(_vm.DataBlockFont),
                nameof(_vm.DataBlockFontSize)),
            ("Map Labels",          nameof(_vm.MapLabelColor),
                true,
                nameof(_vm.MapLabelFont),
                nameof(_vm.MapLabelFontSize)),
        };

        // Build 3-column grid
        int cols = 3;
        int rows = (int)Math.Ceiling(items.Count / (double)cols);

        var colDefs = string.Join(",",
            Enumerable.Repeat("*", cols));
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(colDefs),
            RowDefinitions = new RowDefinitions(
                string.Join(",", Enumerable.Repeat("Auto", rows))),
            Margin = new Avalonia.Thickness(8)
        };

        for (int i = 0; i < items.Count; i++)
        {
            var (label, colorProp, hasFont,
                fontProp, fontSizeProp) = items[i];

            int col = i % cols;
            int row = i / cols;

            var cell = BuildCell(label, colorProp,
                hasFont, fontProp, fontSizeProp);
            Grid.SetColumn(cell, col);
            Grid.SetRow(cell, row);
            grid.Children.Add(cell);
        }

        // Bottom buttons
        var btnOk = new Button
        {
            Content = "OK",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 60,
            Margin = new Avalonia.Thickness(4)
        };
        btnOk.Bind(Button.CommandProperty,
            new Avalonia.Data.Binding(nameof(_vm.OkCommand)));

        var btnApply = new Button
        {
            Content = "Apply",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 60,
            Margin = new Avalonia.Thickness(4)
        };
        btnApply.Bind(Button.CommandProperty,
            new Avalonia.Data.Binding(nameof(_vm.ApplyCommand)));

        var btnUndo = new Button
        {
            Content = "Undo",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 60,
            Margin = new Avalonia.Thickness(4)
        };
        btnUndo.Bind(Button.CommandProperty,
            new Avalonia.Data.Binding(nameof(_vm.UndoCommand)));

        var btnCancel = new Button
        {
            Content = "Cancel",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 60,
            Margin = new Avalonia.Thickness(4)
        };
        btnCancel.Bind(Button.CommandProperty,
            new Avalonia.Data.Binding(nameof(_vm.CancelCommand)));

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };
        buttonRow.Children.Add(btnOk);
        buttonRow.Children.Add(btnApply);
        buttonRow.Children.Add(btnUndo);
        buttonRow.Children.Add(btnCancel);

        var mainStack = new StackPanel
        {
            Margin = new Avalonia.Thickness(4)
        };
        mainStack.Children.Add(grid);
        mainStack.Children.Add(buttonRow);

        PanelContent.Content = mainStack;
    }

    private Control BuildCell(string label, string colorProp,
        bool hasFont, string fontProp, string fontSizeProp)
    {
        // Get current color value via reflection
        var colorVal = typeof(AdaptPanelViewModel)
            .GetProperty(colorProp)?.GetValue(_vm) as string
            ?? "#000000";

        var colorBtn = new Border
        {
            Width = 120,
            Height = 22,
            CornerRadius = new Avalonia.CornerRadius(2),
            Background = new SolidColorBrush(
                Avalonia.Media.Color.Parse(colorVal)),
            Cursor = new Avalonia.Input.Cursor(
                Avalonia.Input.StandardCursorType.Hand),
            BorderBrush = new SolidColorBrush(
                Avalonia.Media.Color.Parse("#545454")),
            BorderThickness = new Avalonia.Thickness(1),
            Child = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Arial"),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = GetContrastColor(colorVal)
            }
        };

        colorBtn.PointerPressed += (_, _) =>
        {
            var currentHex = typeof(AdaptPanelViewModel)
                .GetProperty(colorProp)?.GetValue(_vm) as string
                ?? "#FFFFFF";

            var picker = new ColorPickerWindow(currentHex);
            picker.ColorSelected += (_, hex) =>
            {
                // Set directly on ViewModel instead of via reflection
                switch (colorProp)
                {
                    case nameof(_vm.BackgroundColor):
                        _vm.BackgroundColor = hex; break;
                    case nameof(_vm.BoundaryColor):
                        _vm.BoundaryColor = hex; break;
                    case nameof(_vm.TraconColor):
                        _vm.TraconColor = hex; break;
                    case nameof(_vm.ArtccColor):
                        _vm.ArtccColor = hex; break;
                    case nameof(_vm.AirportColor):
                        _vm.AirportColor = hex; break;
                    case nameof(_vm.VorColor):
                        _vm.VorColor = hex; break;
                    case nameof(_vm.NdbColor):
                        _vm.NdbColor = hex; break;
                    case nameof(_vm.FixColor):
                        _vm.FixColor = hex; break;
                    case nameof(_vm.JetRoutesColor):
                        _vm.JetRoutesColor = hex; break;
                    case nameof(_vm.VictorRoutesColor):
                        _vm.VictorRoutesColor = hex; break;
                    case nameof(_vm.DataBlockColor):
                        _vm.DataBlockColor = hex; break;
                    case nameof(_vm.MapLabelColor):
                        _vm.MapLabelColor = hex; break;
                }

                colorBtn.Background = new SolidColorBrush(
                    Avalonia.Media.Color.Parse(hex));
                if (colorBtn.Child is TextBlock tb)
                    tb.Foreground = GetContrastColor(hex);
            };
            picker.Show(this);
        };

        var cell = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Avalonia.Thickness(4, 3, 4, 3)
        };
        cell.Children.Add(colorBtn);

        if (hasFont)
        {
            var fontBtn = new Button
            {
                Content = "Helv-10",
                FontFamily = new FontFamily("Arial"),
                FontSize = 10,
                Padding = new Avalonia.Thickness(4, 2),
                Margin = new Avalonia.Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            cell.Children.Add(fontBtn);
        }

        return cell;
    }

    private static IBrush GetContrastColor(string hex)
    {
        try
        {
            var c = Avalonia.Media.Color.Parse(hex);
            double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
            return luminance > 128
                ? Brushes.Black
                : Brushes.White;
        }
        catch { return Brushes.Black; }
    }
}