using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using System;

namespace vTFMS.Views.Panels;

public class ArtccThresholdWindow : Window
{
    public event EventHandler<(int yellow, int red)>? ThresholdSet;

    public ArtccThresholdWindow(string identifier, int currentYellow, int currentRed)
    {
        Title = $"{identifier} Thresholds";
        Width = 260;
        Height = 160;
        CanResize = false;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.Parse("#ffe4c4"));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // Apply styles BEFORE building content
        Styles.Add(new Avalonia.Styling.Style(x => x.OfType<TextBlock>())
        {
            Setters =
            {
                new Avalonia.Styling.Setter(
                    TextBlock.ForegroundProperty,
                    new SolidColorBrush(Colors.Black))
            }
        });

        // Yellow row
        var yellowLabel = new TextBlock
        {
            Text = "Yellow at:",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 75
        };

        var yellowBox = new TextBox
        {
            Text = currentYellow.ToString(),
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Width = 60,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            Foreground = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Colors.Yellow)
        };

        var yellowRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(16, 16, 16, 8)
        };
        yellowRow.Children.Add(yellowLabel);
        yellowRow.Children.Add(yellowBox);

        // Red row
        var redLabel = new TextBlock
        {
            Text = "Red at:",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 75
        };

        var redBox = new TextBox
        {
            Text = currentRed.ToString(),
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Width = 60,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            Foreground = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Colors.Red)
        };

        var redRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(16, 8, 16, 16)
        };
        redRow.Children.Add(redLabel);
        redRow.Children.Add(redBox);

        // Buttons
        var okBtn = new Button
        {
            Content = "OK",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Thickness(4),
            Foreground = new SolidColorBrush(Colors.Black)
        };

        var cancelBtn = new Button
        {
            Content = "Cancel",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Thickness(4),
            Foreground = new SolidColorBrush(Colors.Black)
        };

        okBtn.Click += (_, _) =>
        {
            if (int.TryParse(yellowBox.Text, out int y) &&
                int.TryParse(redBox.Text, out int r) &&
                y > 0 && r > 0)
            {
                ThresholdSet?.Invoke(this, (y, r));
                Close();
            }
            else
            {
                if (!int.TryParse(yellowBox.Text, out _))
                    yellowBox.BorderBrush = new SolidColorBrush(Colors.Red);
                if (!int.TryParse(redBox.Text, out _))
                    redBox.BorderBrush = new SolidColorBrush(Colors.Red);
            }
        };

        cancelBtn.Click += (_, _) => Close();

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 8)
        };
        btnRow.Children.Add(okBtn);
        btnRow.Children.Add(cancelBtn);

        var stack = new StackPanel();
        stack.Children.Add(yellowRow);
        stack.Children.Add(redRow);
        stack.Children.Add(btnRow);

        Content = stack;
    }
}