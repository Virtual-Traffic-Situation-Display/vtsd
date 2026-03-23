using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using System;

namespace vTFMS.Views.Panels;

public class SectorCombineWindow : Window
{
    public event EventHandler<string?>? CombineSet;

    public SectorCombineWindow(string sectorId, string? currentCombine)
    {
        Title = $"Combine Sector {sectorId}";
        Width = 260;
        Height = 140;
        CanResize = false;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.Parse("#ffe4c4"));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Styles.Add(new Avalonia.Styling.Style(x => x.OfType<TextBlock>())
        {
            Setters =
            {
                new Avalonia.Styling.Setter(
                    TextBlock.ForegroundProperty,
                    new SolidColorBrush(Colors.Black))
            }
        });

        var label = new TextBlock
        {
            Text = "Combine into sector:",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(16, 16, 16, 4)
        };

        var textBox = new TextBox
        {
            Text = currentCombine ?? string.Empty,
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Colors.White),
            Margin = new Avalonia.Thickness(16, 0, 16, 12),
            Width = 200
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
            var value = textBox.Text?.Trim();
            CombineSet?.Invoke(this,
                string.IsNullOrWhiteSpace(value) ? null : value);
            Close();
        };

        cancelBtn.Click += (_, _) => Close();

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        btnRow.Children.Add(okBtn);
        btnRow.Children.Add(cancelBtn);

        var stack = new StackPanel();
        stack.Children.Add(label);
        stack.Children.Add(textBox);
        stack.Children.Add(btnRow);

        Content = stack;
    }
}