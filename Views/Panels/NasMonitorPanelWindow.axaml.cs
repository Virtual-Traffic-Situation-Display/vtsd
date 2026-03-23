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
    private readonly IMapDataService _mapDataService;
    private CancellationTokenSource? _rebuildDebounce;

    private bool _sectorViewActive = false;
    private string? _selectedArtcc = null;
    private bool _hideBaselines = false;

    public NasMonitorPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes TsdViewModel and IMapDataService.");
    }

    public NasMonitorPanelWindow(
        TsdViewModel tsdViewModel,
        IMapDataService mapDataService)
    {
        _mapDataService = mapDataService;
        _vm = new NasMonitorPanelViewModel(tsdViewModel, mapDataService);
        DataContext = _vm;
        InitializeComponent();

        var artccSelector = this.FindControl<ComboBox>("ArtccSelector");
        if (artccSelector != null)
        {
            var artccs = _mapDataService.GetArtccsWithSectors();
            artccSelector.ItemsSource = artccs;
            if (artccs.Count > 0)
                artccSelector.SelectedIndex = 0;
        }

        DataScroll.ScrollChanged += (_, e) =>
            HeaderScroll.Offset = new Avalonia.Vector(
                DataScroll.Offset.X, HeaderScroll.Offset.Y);

        HeaderScroll.ScrollChanged += (_, e) =>
            DataScroll.Offset = new Avalonia.Vector(
                HeaderScroll.Offset.X, DataScroll.Offset.Y);

        _vm.Rows.CollectionChanged += (_, _) => RebuildTable();
        _vm.MonitoredTracons.CollectionChanged += (_, _) => RebuildTable();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.TimeLabels) ||
                e.PropertyName == nameof(_vm.Rows) ||
                e.PropertyName == nameof(_vm.SectorCounts) ||
                e.PropertyName == nameof(_vm.TraconCounts))
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

    private void SummaryView_Checked(object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        _sectorViewActive = false;

        var artccSelector = this.FindControl<ComboBox>("ArtccSelector");
        if (artccSelector != null) artccSelector.IsVisible = false;

        var sectorToolbar = this.FindControl<StackPanel>("SectorToolbar");
        if (sectorToolbar != null) sectorToolbar.IsVisible = false;

        RebuildTable();
    }

    private void SectorView_Checked(object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        _sectorViewActive = true;

        var artccSelector = this.FindControl<ComboBox>("ArtccSelector");
        if (artccSelector != null)
        {
            artccSelector.IsVisible = true;
            _selectedArtcc = artccSelector.SelectedItem as string;
        }

        var sectorToolbar = this.FindControl<StackPanel>("SectorToolbar");
        if (sectorToolbar != null) sectorToolbar.IsVisible = true;

        RebuildTable();
    }

    private void ArtccSelector_SelectionChanged(object? sender,
        SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb)
            _selectedArtcc = cb.SelectedItem as string;

        if (_sectorViewActive)
            RebuildTable();
    }

    private void CombineSectors_Click(object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var existing = _vm.CombineRules
            .Select(r => (r.Parent, string.Join(" ", r.Children)))
            .ToList();

        var window = new CombineSectorsWindow(existing);
        window.RulesSet += (_, rules) =>
        {
            var newRules = rules.Select(r => new SectorCombineRule
            {
                Parent = r.parent,
                Children = r.children
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
            }).ToList();

            _vm.SetCombineRules(newRules);
            RebuildTable();
        };
        window.ShowDialog(this);
    }

    private void HideBaselines_Click(object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        _hideBaselines = !_hideBaselines;

        if (sender is Button btn)
            btn.Content = _hideBaselines
                ? "Show All Baselines"
                : "Hide All Baselines";

        RebuildTable();
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
        if (_sectorViewActive && !string.IsNullOrEmpty(_selectedArtcc))
            DoRebuildSectorTable();
        else
            DoRebuildSummaryTable();
    }

    private void DoRebuildSummaryTable()
    {
        var headerRow = this.FindControl<ItemsControl>("HeaderRow");
        var dataRows = this.FindControl<ItemsControl>("DataRows");
        if (headerRow == null || dataRows == null) return;

        int colCount = _vm.TimeLabels.Count;
        string colDefs = "60," +
            string.Join(",", Enumerable.Repeat("50", colCount));

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

        var rows = new List<Control>();
        foreach (var row in _vm.Rows)
        {
            var rowGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions(colDefs)
            };

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

            var threshold = _vm.GetThreshold(row.CenterId);

            int capturedArtcc = _vm.Rows.IndexOf(row);

            for (int i = 0; i < colCount && i < row.Cells.Count; i++)
            {
                int count = _vm.IsEnabled ? row.Cells[i].Count : -1;
                var (bg, fg, text) = GetCellStyle(
                    count, _vm.IsEnabled,
                    threshold.YellowAt,
                    threshold.RedAt);

                var cellBorder = MakeCell(text, i + 1, false, bg, fg);

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

        // TRACON rows
        foreach (var config in _vm.MonitoredTracons)
        {
            var rowGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions(colDefs)
            };

            var labelCell = MakeCell(
                config.Identifier, 0, false, "#006699", "#ffffff");

            // Double-tap to set threshold
            labelCell.DoubleTapped += (_, _) =>
            {
                var threshold = _vm.GetThreshold(config.Identifier);
                var popup = new ArtccThresholdWindow(
                    config.Identifier,
                    threshold.YellowAt,
                    threshold.RedAt);

                popup.ThresholdSet += (_, t) =>
                {
                    _vm.SetThreshold(config.Identifier, t.yellow, t.red);
                    RebuildTable();
                };

                popup.ShowDialog(this);
            };

            // Right-click to remove
            labelCell.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton ==
                    Avalonia.Input.MouseButton.Right)
                {
                    _vm.RemoveTraconCommand.Execute(config);
                    RebuildTable();
                }
            };

            rowGrid.Children.Add(labelCell);

            var threshold = _vm.GetThreshold(config.Identifier);

            for (int i = 0; i < colCount; i++)
            {
                _vm.TraconCounts.TryGetValue(
                    config.Identifier, out var counts);
                int count = _vm.IsEnabled
                    ? (counts != null ? counts[i] : 0)
                    : -1;
                var (bg, fg, text) = GetCellStyle(
                    count, _vm.IsEnabled,
                    threshold.YellowAt,
                    threshold.RedAt);
                rowGrid.Children.Add(MakeCell(text, i + 1, false, bg, fg));
            }

            rows.Add(rowGrid);
        }

        dataRows.ItemsSource = rows;
    }

    private void DoRebuildSectorTable()
    {
        var headerRow = this.FindControl<ItemsControl>("HeaderRow");
        var dataRows = this.FindControl<ItemsControl>("DataRows");
        if (headerRow == null || dataRows == null) return;
        if (string.IsNullOrEmpty(_selectedArtcc)) return;

        var sectors = _mapDataService.GetSectorsForArtcc(_selectedArtcc);
        int colCount = _vm.TimeLabels.Count;
        string colDefs = "60," +
            string.Join(",", Enumerable.Repeat("50", colCount));

        // Header row
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(colDefs)
        };
        headerGrid.Children.Add(
            MakeCell("Sector", 0, true, "#ffe4c4", "#000000"));
        for (int i = 0; i < colCount; i++)
            headerGrid.Children.Add(
                MakeCell(_vm.TimeLabels[i], i + 1, true, "#ffe4c4", "#000000"));
        headerRow.ItemsSource = new[] { headerGrid };

        var uniqueSectors = sectors
            .GroupBy(s => s.Sector)
            .OrderBy(g => g.Key)
            .ToList();

        var parentSectors = new HashSet<string>(
            _vm.CombineRules.Select(r => r.Parent),
            StringComparer.OrdinalIgnoreCase);
        var childSectors = new HashSet<string>(
            _vm.CombineRules.SelectMany(r => r.Children),
            StringComparer.OrdinalIgnoreCase);

        var orderedRows = new List<(string sectorNum, bool isCombinedRow, bool isChild)>();
        var placed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sectorGroup in uniqueSectors)
        {
            string sectorNum = sectorGroup.Key;
            if (placed.Contains(sectorNum)) continue;

            bool isParent = parentSectors.Contains(sectorNum);
            bool isChild = childSectors.Contains(sectorNum);

            if (isChild && !isParent) continue;

            if (isParent)
            {
                orderedRows.Add((sectorNum, true, false));
                placed.Add(sectorNum);

                var rule = _vm.CombineRules.FirstOrDefault(r =>
                    r.Parent.Equals(sectorNum,
                        StringComparison.OrdinalIgnoreCase));
                if (rule != null)
                {
                    foreach (var child in rule.Children)
                    {
                        orderedRows.Add((child, false, true));
                        placed.Add(child);
                    }
                }
            }
            else
            {
                orderedRows.Add((sectorNum, false, false));
                placed.Add(sectorNum);
            }
        }

        var rows = new List<Control>();
        string artcc = _selectedArtcc!;

        foreach (var (sectorNum, isCombinedRow, isChild) in orderedRows)
        {
            if (_hideBaselines && isChild) continue;

            if (isCombinedRow)
            {
                string combinedKey = $"{artcc}-{sectorNum}+".ToUpperInvariant();
                _vm.SectorCounts.TryGetValue(combinedKey, out var combinedCounts);
                var threshold = _vm.GetThreshold(combinedKey);

                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(colDefs)
                };
                rowGrid.Children.Add(
                    MakeCell($"{sectorNum}+", 0, false, "#0000CC", "#ffffff"));

                for (int i = 0; i < colCount; i++)
                {
                    int count = _vm.IsEnabled
                        ? (combinedCounts != null ? combinedCounts[i] : 0)
                        : -1;
                    var (bg, fg, text) = GetCellStyle(
                        count, _vm.IsEnabled,
                        threshold.YellowAt,
                        threshold.RedAt);
                    rowGrid.Children.Add(MakeCell(text, i + 1, false, bg, fg));
                }

                rows.Add(rowGrid);
            }
            else
            {
                string sectorKey = $"{artcc}-{sectorNum}".ToUpperInvariant();
                _vm.SectorCounts.TryGetValue(sectorKey, out var counts);
                var threshold = _vm.GetThreshold(sectorKey);

                string rowBg = isChild ? "#808080" : "#ffffff";
                string rowFg = isChild ? "#ffffff" : "#000000";

                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(colDefs)
                };
                rowGrid.Children.Add(
                    MakeCell(isChild ? $"({sectorNum})" : sectorNum, 0, false, rowBg, rowFg));

                for (int i = 0; i < colCount; i++)
                {
                    int count = _vm.IsEnabled
                        ? (counts != null ? counts[i] : 0)
                        : -1;

                    if (isChild)
                    {
                        var (_, _, text) = GetCellStyle(
                            count, _vm.IsEnabled,
                            threshold.YellowAt,
                            threshold.RedAt);
                        rowGrid.Children.Add(
                            MakeCell(text, i + 1, false, "#808080", "#ffffff"));
                    }
                    else
                    {
                        var (bg, fg, text) = GetCellStyle(
                            count, _vm.IsEnabled,
                            threshold.YellowAt,
                            threshold.RedAt);
                        rowGrid.Children.Add(
                            MakeCell(text, i + 1, false, bg, fg));
                    }
                }

                rows.Add(rowGrid);
            }
        }

        dataRows.ItemsSource = rows;
    }

    private static (string bg, string fg, string text) GetCellStyle(
        int count, bool isEnabled, int yellowAt, int redAt)
    {
        if (!isEnabled)
            return ("#808080", "#808080", "");
        if (count == 0)
            return ("#00AA00", "#000000", "0");
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
                FontWeight = Avalonia.Media.FontWeight.Bold,
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
