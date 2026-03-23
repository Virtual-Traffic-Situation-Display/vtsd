using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace vTFMS.Views.Panels;

public class CombineSectorsWindow : Window
{
    public event EventHandler<List<(string parent, string children)>>? RulesSet;

    private readonly StackPanel _rulesPanel;

    public CombineSectorsWindow(
        List<(string parent, string children)> existingRules)
    {
        Title = "Combine Sectors";
        Width = 400;
        Height = 350;
        CanResize = true;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.Parse("#ffe4c4"));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Styles.Add(new Avalonia.Styling.Style(
            x => x.OfType<TextBlock>())
        {
            Setters =
            {
                new Avalonia.Styling.Setter(
                    TextBlock.ForegroundProperty,
                    new SolidColorBrush(Colors.Black))
            }
        });

        _rulesPanel = new StackPanel { Spacing = 4 };

        foreach (var rule in existingRules)
            AddRow(rule.parent, rule.children);

        var addBtn = new Button
        {
            Content = "+ Add Rule",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Black),
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };
        addBtn.Click += (_, _) => AddRow("", "");

        var scroll = new ScrollViewer
        {
            Content = _rulesPanel,
            VerticalScrollBarVisibility =
                Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Margin = new Avalonia.Thickness(8, 8, 8, 4)
        };

        var okBtn = new Button
        {
            Content = "OK",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Avalonia.Thickness(4),
            Foreground = new SolidColorBrush(Colors.Black)
        };

        var cancelBtn = new Button
        {
            Content = "Cancel",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Avalonia.Thickness(4),
            Foreground = new SolidColorBrush(Colors.Black)
        };

        okBtn.Click += (_, _) =>
        {
            var rules = new List<(string, string)>();
            foreach (var child in _rulesPanel.Children)
            {
                if (child is Grid grid)
                {
                    var parentBox = grid.Children
                        .OfType<TextBox>().FirstOrDefault();
                    var childBox = grid.Children
                        .OfType<TextBox>().LastOrDefault();
                    if (parentBox != null && childBox != null &&
                        !string.IsNullOrWhiteSpace(parentBox.Text))
                        rules.Add((
                            parentBox.Text.Trim(),
                            childBox.Text?.Trim() ?? ""));
                }
            }
            RulesSet?.Invoke(this, rules);
            Close();
        };

        cancelBtn.Click += (_, _) => Close();

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 4, 0, 8)
        };
        btnRow.Children.Add(okBtn);
        btnRow.Children.Add(cancelBtn);

        var main = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto,Auto")
        };
        Grid.SetRow(scroll, 0);
        Grid.SetRow(addBtn, 1);
        Grid.SetRow(btnRow, 2);
        main.Children.Add(scroll);
        main.Children.Add(addBtn);
        main.Children.Add(btnRow);

        Content = main;
    }

    private void AddRow(string parent, string children)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("80,*,Auto"),
            Margin = new Avalonia.Thickness(0, 2)
        };

        var parentBox = new TextBox
        {
            Text = parent,
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Colors.White),
            Watermark = "Parent",
            Margin = new Avalonia.Thickness(0, 0, 4, 0)
        };

        var childBox = new TextBox
        {
            Text = children,
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Colors.White),
            Watermark = "Children (space separated)",
            Margin = new Avalonia.Thickness(0, 0, 4, 0)
        };

        var deleteBtn = new Button
        {
            Content = "✕",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Black),
            Padding = new Avalonia.Thickness(6, 2)
        };
        deleteBtn.Click += (_, _) => _rulesPanel.Children.Remove(grid);

        Grid.SetColumn(parentBox, 0);
        Grid.SetColumn(childBox, 1);
        Grid.SetColumn(deleteBtn, 2);

        grid.Children.Add(parentBox);
        grid.Children.Add(childBox);
        grid.Children.Add(deleteBtn);

        _rulesPanel.Children.Add(grid);
    }
}