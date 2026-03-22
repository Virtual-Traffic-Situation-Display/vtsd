using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using vTFMS.Models;
using vTFMS.Services;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class NasMonitorPanelWindow : BasePanelWindow
{
    private readonly NasMonitorPanelViewModel _vm;
    private CancellationTokenSource? _rebuildDebounce;

    public NasMonitorPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes TsdViewModel and IMapDataService.");
    }

    public NasMonitorPanelWindow(
        TsdViewModel tsdViewModel,
        IMapDataService mapDataService)
    {
        _vm = new NasMonitorPanelViewModel(tsdViewModel, mapDataService);
        DataContext = _vm;
        InitializeComponent();

        DataScroll.ScrollChanged += (_, e) =>
            HeaderScroll.Offset = new Avalonia.Vector(
                DataScroll.Offset.X, HeaderScroll.Offset.Y);

        HeaderScroll.ScrollChanged += (_, e) =>
            DataScroll.Offset = new Avalonia.Vector(
                HeaderScroll.Offset.X, DataScroll.Offset.Y);

        _vm.Rows.CollectionChanged += (_, _) => RebuildTable();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.TimeLabels) ||
                e.PropertyName == nameof(_vm.Rows))
                RebuildTable();
        };

        Closed += (_, _) =>
        {
            _rebuildDebounce?.Cancel();
            _vm.Dispose();
        };

        RebuildTable();
    }

    private void HorizonSlider_PointerReleased(object? sender,
        Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (sender is Slider slider)
            _vm.HorizonMinutes = (int)Math.Round(slider.Value / 15) * 15;
    }

    private void RebuildTable()
    {
        _rebuildDebounce?.Cancel();
        _rebuildDebounce = new CancellationTokenSource();
        var token = _rebuildDebounce.Token;

        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await Task.Delay(200, token);
                if (token.IsCancellationRequested) return;
                DoRebuildTable();
            }
            catch (OperationCanceledException) { }
        });
    }

    private void DoRebuildTable()
    {
        var headerRow = this.FindControl<ItemsControl>("HeaderRow");
        var dataRows = this.FindControl<ItemsControl>("DataRows");
        if (headerRow == null || dataRows == null) return;

        int colCount = _vm.TimeLabels.Count;
        string colDefs = "60," +
            string.Join(",", Enumerable.Repeat("50", colCount));

        // Header row
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(colDefs)
        };
        headerGrid.Children.Add(
            MakeCell("Center", 0, true, "#ffe4c4", "#000000"));
        for (int i = 0; i < colCount; i++)
            headerGrid.Children.Add(
                MakeCell(_vm.TimeLabels[i], i + 1, true, "#ffe4c4", "#000000"));

        headerRow.ItemsSource = new[] { headerGrid };

        // Data rows
        var rows = new List<Control>();
        foreach (var row in _vm.Rows)
        {
            var rowGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions(colDefs)
            };

            // Center ID cell with double-click threshold editor
            var centerBorder = MakeCell(
                row.CenterId, 0, false, "#ffffff", "#000000");

            centerBorder.DoubleTapped += (_, _) =>
            {
                var threshold = _vm.GetThreshold(row.CenterId);
                var popup = new ArtccThresholdWindow(
                    row.CenterId,
                    threshold.YellowAt,
                    threshold.RedAt);

                popup.ThresholdSet += (_, t) =>
                {
                    _vm.SetThreshold(row.CenterId, t.yellow, t.red);
                    RebuildTable();
                };

                popup.ShowDialog(this);
            };

            rowGrid.Children.Add(centerBorder);

            // Count cells using per-ARTCC thresholds
            var artccThreshold = _vm.GetThreshold(row.CenterId);
            int artccIndex = _vm.Rows.IndexOf(row);
            for (int i = 0; i < row.Cells.Count && i < colCount; i++)
            {
                var cell = row.Cells[i];
                var (bg, fg, text) = GetCellStyle(
                    cell.Count, _vm.IsEnabled,
                    artccThreshold.YellowAt,
                    artccThreshold.RedAt);

                var cellBorder = MakeCell(text, i + 1, false, bg, fg);

                // Capture for closure
                int capturedArtcc = artccIndex;
                int capturedCol = i;
                string capturedCenter = row.CenterId;
                string capturedLabel = i < _vm.TimeLabels.Count
                    ? _vm.TimeLabels[i] : string.Empty;

                cellBorder.PointerReleased += (_, e) =>
                {
                    if (e.InitialPressMouseButton ==
                        Avalonia.Input.MouseButton.Right)
                    {
                        var details = _vm.GetCellDetails(
                            capturedArtcc, capturedCol);
                        var popup = new CellDetailWindow(
                            capturedCenter, capturedLabel, details);
                        popup.ShowDialog(this);
                    }
                };

                rowGrid.Children.Add(cellBorder);
            }

            rows.Add(rowGrid);
        }

        dataRows.ItemsSource = rows;
    }

    private static (string bg, string fg, string text) GetCellStyle(
        int count, bool isEnabled, int yellowAt, int redAt)
    {
        if (!isEnabled)
            return ("#808080", "#808080", "");
        if (count == 0)
            return ("#00AA00", "#00AA00", "");
        if (count >= redAt)
            return ("#FF0000", "#000000", count.ToString());
        if (count >= yellowAt)
            return ("#FFFF00", "#000000", count.ToString());
        return ("#00AA00", "#000000", count.ToString());
    }

    private static Control MakeCell(string text,
        int column, bool isHeader, string bgHex, string fgHex)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse(bgHex)),
            BorderBrush = new SolidColorBrush(Color.Parse("#aaaaaa")),
            BorderThickness = new Avalonia.Thickness(
                column == 0 ? 1 : 0, 1, 1, 1)
        };

        if (!string.IsNullOrEmpty(text))
        {
            border.Child = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Courier New"),
                FontSize = 11,
                FontWeight = isHeader
                    ? Avalonia.Media.FontWeight.Bold
                    : Avalonia.Media.FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse(fgHex)),
                HorizontalAlignment = isHeader
                    ? Avalonia.Layout.HorizontalAlignment.Center
                    : Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(4, 2)
            };
        }

        Grid.SetColumn(border, column);
        return border;
    }
}