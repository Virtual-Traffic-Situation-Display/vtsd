using Avalonia.Controls;
using Avalonia.Media;
using System.Linq;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;
using System;

namespace vTFMS.Views.Panels;

public partial class FlightCountPanelWindow : BasePanelWindow
{
    private readonly FlightCountPanelViewModel _vm;
    private readonly TsdViewModel _tsdViewModel;

    public FlightCountPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes TsdViewModel.");
    }

    public FlightCountPanelWindow(TsdViewModel tsdViewModel)
    {
        _tsdViewModel = tsdViewModel;
        _vm = new FlightCountPanelViewModel(tsdViewModel);
        DataContext = _vm;
        InitializeComponent();
        BuildContent();

        if (DataContext is BasePanelViewModel vm)
            vm.IsPinned = true;
    }

    private void BuildContent()
    {
        var scroll = new ScrollViewer
        {
            Margin = new Avalonia.Thickness(4)
        };

        var stack = new StackPanel();

        void RebuildStack()
        {
            stack.Children.Clear();

            string bgColor = _tsdViewModel.DisplaySettings.BackgroundColor;

            // Header — always white background, black text
            stack.Children.Add(BuildRow(
                "Arr", "Dep", "Actv", "Visib",
                isHeader: true,
                textColor: "#000000",
                bgHex: "#ffffff"));

            // ALL row — background color, contrasting text
            var allRow = _vm.Rows.FirstOrDefault();
            if (allRow != null)
            {
                string contrastColor = GetContrastColor(bgColor);
                stack.Children.Add(BuildRow(
                    allRow.Arr, allRow.Dep,
                    allRow.Active.ToString(),
                    allRow.Visible.ToString(),
                    isHeader: false,
                    textColor: contrastColor,
                    bgHex: bgColor));
            }

            // Filter rows — background color, filter text color
            var filters = _tsdViewModel.SelectFlightsViewModel
                .Filters
                .Where(f => f.Show &&
                    (!string.IsNullOrWhiteSpace(f.Arrival) ||
                     !string.IsNullOrWhiteSpace(f.Departure)))
                .ToList();

            for (int i = 1; i < _vm.Rows.Count; i++)
            {
                var row = _vm.Rows[i];
                var color = i - 1 < filters.Count
                    ? filters[i - 1].Color
                    : "#000000";

                stack.Children.Add(BuildRow(
                    row.Arr, row.Dep,
                    row.Active.ToString(),
                    row.Visible.ToString(),
                    isHeader: false,
                    textColor: color,
                    bgHex: bgColor));
            }
        }

        RebuildStack();

        _vm.Rows.CollectionChanged += (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(RebuildStack);

        scroll.Content = stack;
        PanelBody = scroll;
    }

    // Returns black or white depending on which contrasts better with bgHex
    private static string GetContrastColor(string bgHex)
    {
        try
        {
            var color = Color.Parse(bgHex);
            // Perceived luminance formula
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance > 0.5 ? "#000000" : "#ffffff";
        }
        catch
        {
            return "#000000";
        }
    }

    private static Control BuildRow(string arr, string dep,
        string active, string visible, bool isHeader,
        string textColor = "#000000", string bgHex = "#ffffff")
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse(bgHex)),
            BorderBrush = new SolidColorBrush(Color.Parse("#aaaaaa")),
            BorderThickness = new Avalonia.Thickness(0, 0, 0, 1)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*")
        };

        var font = new FontFamily("Courier New");
        var foreground = new SolidColorBrush(Color.Parse(textColor));

        var texts = new[] { arr, dep, active, visible };
        for (int i = 0; i < texts.Length; i++)
        {
            bool isNumber = i >= 2 && !isHeader;

            var tb = new TextBlock
            {
                Text = texts[i],
                FontFamily = font,
                FontSize = 11,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = foreground,
                Margin = new Avalonia.Thickness(6, 3),
                HorizontalAlignment = isNumber
                    ? Avalonia.Layout.HorizontalAlignment.Right
                    : Avalonia.Layout.HorizontalAlignment.Left
            };

            var cell = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#aaaaaa")),
                BorderThickness = new Avalonia.Thickness(
                    i == 0 ? 0 : 1, 0, 0, 0),
                Child = tb
            };

            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
        }

        border.Child = grid;
        return border;
    }
}