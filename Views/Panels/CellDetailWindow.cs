using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.Generic;
using vTFMS.Models;

namespace vTFMS.Views.Panels;

public class CellDetailWindow : Window
{
    public CellDetailWindow(string artccId, string timeLabel,
        List<(VatsimPilot pilot, LatLon pos)> entries)
    {
        Title = $"{artccId} — {timeLabel}";
        Width = 500;
        Height = 400;
        CanResize = true;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.Parse("#ffe4c4"));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{artccId} at {timeLabel}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine(
            $"{"Callsign",-10} {"Lat",9} {"Lon",10} {"Alt",7} " +
            $"{"Dep",-5} {"Arr",-5}");
        sb.AppendLine(new string('-', 60));

        foreach (var (pilot, pos) in entries)
        {
            sb.AppendLine(
                $"{pilot.Callsign,-10} " +
                $"{pos.Lat,9:F3} " +
                $"{pos.Lon,10:F3} " +
                $"{pilot.Altitude,7} " +
                $"{pilot.Departure,-5} " +
                $"{pilot.Arrival,-5}");
        }

        var textBox = new TextBox
        {
            Text = sb.ToString(),
            IsReadOnly = true,
            FontFamily = new FontFamily("Courier New"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Black),
            Background = new SolidColorBrush(Color.Parse("#ffe4c4")),
            BorderThickness = new Avalonia.Thickness(0),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var scroll = new ScrollViewer
        {
            Content = textBox,
            HorizontalScrollBarVisibility =
                Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility =
                Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Margin = new Avalonia.Thickness(8)
        };

        Content = scroll;
    }
}