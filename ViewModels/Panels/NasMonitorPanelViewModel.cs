using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using vTFMS.Models;
using vTFMS.Services;

namespace vTFMS.ViewModels.Panels;

public class NasMonitorCell
{
    public int Count { get; set; } = -1;
    public List<VatsimPilot> Pilots { get; set; } = new();
}

public class NasMonitorRow
{
    public string CenterId { get; set; } = string.Empty;
    public List<NasMonitorCell> Cells { get; set; } = new();
}

public partial class NasMonitorPanelViewModel : BasePanelViewModel, IDisposable
{
    private readonly TsdViewModel _tsdViewModel;
    private readonly IMapDataService _mapDataService;
    private string? _lastSavePath;

    [ObservableProperty]
    private bool _isEnabled = false;

    public string EnableButtonText =>
        IsEnabled ? "Disable" : "Enable";

    private Dictionary<string, ArtccThreshold> _thresholds = new();

    public ObservableCollection<TraconMonitorConfig> MonitoredTracons { get; } = new();
    public Dictionary<string, int[]> TraconCounts { get; private set; } = new();

    [ObservableProperty]
    private string _traconIdentifier = string.Empty;

    [ObservableProperty]
    private int _traconAltitude = 99999;

    // Combine rules — scoped per ARTCC
    public List<SectorCombineRule> CombineRules { get; private set; } = new();

    public void SetCombineRules(List<SectorCombineRule> rules)
    {
        CombineRules = rules;
        if (IsEnabled) _ = RecalculateAsync();
        else OnPropertyChanged(nameof(SectorCounts));
    }

    // Sector counts — key: "ZJX-02", value: count per time slot
    // Combined rows use key: "ZJX-50+"
    public Dictionary<string, int[]> SectorCounts { get; private set; } = new();

    // ARTCC summary counts
    public Dictionary<string, int[]> ArtccCounts { get; private set; } = new();

    private Dictionary<(int, int), List<(VatsimPilot pilot, LatLon pos)>>
        _cellDetails = new();

    private class TupleStringComparer : IEqualityComparer<(string, string)>
    {
        public bool Equals((string, string) x, (string, string) y) =>
            x.Item1 == y.Item1 && x.Item2 == y.Item2;
        public int GetHashCode((string, string) obj) =>
            HashCode.Combine(obj.Item1, obj.Item2);
    }

    public ArtccThreshold GetThreshold(string identifier)
    {
        if (_thresholds.TryGetValue(identifier, out var t))
            return t;
        return new ArtccThreshold
        {
            Identifier = identifier,
            YellowAt = 12,
            RedAt = 20
        };
    }

    public void SetThreshold(string identifier, int yellowAt, int redAt)
    {
        _thresholds[identifier] = new ArtccThreshold
        {
            Identifier = identifier,
            YellowAt = yellowAt,
            RedAt = redAt
        };
    }

    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(EnableButtonText));
        ShowResourceWarning = HorizonMinutes > 180;
        if (value)
            _ = RecalculateAsync();
        else
            ResetCells();
    }

    private int _horizonMinutes = 60;
    public int HorizonMinutes
    {
        get => _horizonMinutes;
        set
        {
            if (_horizonMinutes == value) return;
            _horizonMinutes = value;
            OnPropertyChanged();
            ShowResourceWarning = value > 180;
            BuildTimeLabels();
            InitializeRows();
            if (IsEnabled) _ = RecalculateAsync();
        }
    }

    [ObservableProperty]
    private bool _showResourceWarning = false;

    public ObservableCollection<NasMonitorRow> Rows { get; } = new();
    public List<string> TimeLabels { get; private set; } = new();

    private readonly EventHandler _onPilotsRefreshed;

    public NasMonitorPanelViewModel(
        TsdViewModel tsdViewModel,
        IMapDataService mapDataService)
    {
        Title = "NAS Monitor";
        _tsdViewModel = tsdViewModel;
        _mapDataService = mapDataService;

        _onPilotsRefreshed = (_, _) => { if (IsEnabled) _ = RecalculateAsync(); };
        _tsdViewModel.PilotsRefreshed += _onPilotsRefreshed;

        InitializeRows();
        BuildTimeLabels();
    }

    public void Dispose()
    {
        _tsdViewModel.PilotsRefreshed -= _onPilotsRefreshed;
    }

    public List<(VatsimPilot pilot, LatLon pos)> GetCellDetails(
        int rowIndex, int colIndex)
    {
        if (_cellDetails.TryGetValue((rowIndex, colIndex), out var list))
            return list;
        return new();
    }

    private void InitializeRows()
    {
        Rows.Clear();
        _cellDetails.Clear();
        foreach (var artcc in _tsdViewModel.ArtccBoundaries)
        {
            var row = new NasMonitorRow { CenterId = artcc.Identifier };
            int colCount = GetColumnCount();
            for (int i = 0; i < colCount; i++)
                row.Cells.Add(new NasMonitorCell { Count = -1 });
            Rows.Add(row);
        }
    }

    private int GetColumnCount()
    {
        var now = DateTime.UtcNow;
        int minutesToNext = now.Minute % 15 == 0 ? 15 : 15 - (now.Minute % 15);
        var next = now.AddMinutes(minutesToNext);
        next = new DateTime(next.Year, next.Month, next.Day,
            next.Hour, (next.Minute / 15) * 15, 0);

        int count = 1;
        var t = next;
        while ((t - now).TotalMinutes < HorizonMinutes)
        {
            count++;
            t = t.AddMinutes(15);
        }
        return count;
    }

    private void BuildTimeLabels()
    {
        var now = DateTime.UtcNow;
        TimeLabels = new List<string> { $"{now:HHmm}" };

        int minutesToNext = now.Minute % 15 == 0 ? 15 : 15 - (now.Minute % 15);
        var next = now.AddMinutes(minutesToNext);
        next = new DateTime(next.Year, next.Month, next.Day,
            next.Hour, (next.Minute / 15) * 15, 0);

        var t = next;
        while ((t - now).TotalMinutes < HorizonMinutes)
        {
            TimeLabels.Add($"{t:HHmm}");
            t = t.AddMinutes(15);
        }

        OnPropertyChanged(nameof(TimeLabels));
    }

    [RelayCommand]
    private async Task ToggleEnable()
    {
        IsEnabled = !IsEnabled;
    }

    private void ResetCells()
    {
        foreach (var row in Rows)
            foreach (var cell in row.Cells)
            {
                cell.Count = -1;
                cell.Pilots.Clear();
            }
        _cellDetails.Clear();
        SectorCounts.Clear();
        ArtccCounts.Clear();
        TraconCounts.Clear();
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(SectorCounts));
        OnPropertyChanged(nameof(ArtccCounts));
        OnPropertyChanged(nameof(TraconCounts));
    }

    public async Task RecalculateAsync()
    {
        if (!IsEnabled) return;

        BuildTimeLabels();

        var newColCount = GetColumnCount();
        if (newColCount != Rows.FirstOrDefault()?.Cells.Count)
            InitializeRows();

        var pilots = _tsdViewModel.AllCurrentPilots.ToList();
        await _tsdViewModel.ResolveAllRoutesAsync(pilots);

        var artccList = _tsdViewModel.ArtccBoundaries.ToList();
        var rules = CombineRules.ToList();
        var monitoredTracons = MonitoredTracons.ToList();

        // Normalize parent and children in the lookup build
        var childToParent = new Dictionary<(string, string), string>(
            new TupleStringComparer());
        foreach (var rule in rules)
        {
            string normalizedParent = rule.Parent.TrimStart('0').PadLeft(1, '0');
            foreach (var child in rule.Children)
            {
                string normalizedChild = child.TrimStart('0').PadLeft(1, '0');
                if (!string.IsNullOrEmpty(rule.Artcc))
                    childToParent[(
                        rule.Artcc.ToUpperInvariant(),
                        normalizedChild)] = normalizedParent;
                if (!childToParent.ContainsKey(("", normalizedChild)))
                    childToParent[("", normalizedChild)] = normalizedParent;
            }
        }

        var now = DateTime.UtcNow;
        int minutesToNext = now.Minute % 15 == 0 ? 15 : 15 - (now.Minute % 15);
        var next = now.AddMinutes(minutesToNext);
        next = new DateTime(next.Year, next.Month, next.Day,
            next.Hour, (next.Minute / 15) * 15, 0);

        var timeSlots = new List<int> { 0 };
        var t = next;
        while ((t - now).TotalMinutes < HorizonMinutes)
        {
            timeSlots.Add((int)(t - now).TotalMinutes);
            t = t.AddMinutes(15);
        }

        int colCount = timeSlots.Count;
        int[] minutes = timeSlots.ToArray();

        var (artccCounts, sectorCounts, details, traconCounts) =
            await Task.Run(() =>
            {
                var artccResult = new Dictionary<string, int[]>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (var artcc in artccList)
                    artccResult[artcc.Identifier] = new int[colCount];

                var sectorResult = new Dictionary<string, int[]>(
                    StringComparer.OrdinalIgnoreCase);

                var detailMap = new Dictionary<(int, int),
                    List<(VatsimPilot, LatLon)>>();

                foreach (var pilot in pilots)
                {
                    if (pilot.GroundSpeed <= 35) continue;

                    try
                    {
                        for (int col = 0; col < colCount; col++)
                        {
                            LatLon? pos;
                            int estimatedAlt;

                            if (minutes[col] == 0)
                            {
                                pos = new LatLon(pilot.Lat, pilot.Lon);
                                estimatedAlt = pilot.Altitude;
                            }
                            else
                            {
                                pos = RouteProjector.ProjectPosition(
                                    pilot, minutes[col]);
                                if (pos == null) continue;
                                estimatedAlt = _mapDataService.EstimateAltitude(
                                    pilot, pos);
                            }

                            var sector = _mapDataService.FindSectorForPosition(
                                pos.Lat, pos.Lon, estimatedAlt);

                            if (sector == null) continue;

                            string artcc = sector.Artcc;
                            string sectorNum = sector.Sector;

                            // Individual sector count
                            var individualKey =
                                $"{artcc}-{sectorNum}".ToUpperInvariant();
                            if (!sectorResult.ContainsKey(individualKey))
                                sectorResult[individualKey] = new int[colCount];
                            sectorResult[individualKey][col]++;

                            // Check if this sector is a parent in a combine
                            // rule — match on specific ARTCC or empty ARTCC
                            bool isParent = rules.Any(r =>
                                (string.IsNullOrEmpty(r.Artcc) ||
                                 r.Artcc.Equals(artcc, StringComparison.OrdinalIgnoreCase)) &&
                                r.Parent.TrimStart('0').PadLeft(1, '0').Equals(
                                    sectorNum, StringComparison.OrdinalIgnoreCase));

                            // Look up parent — try specific ARTCC first,
                            // then fall back to empty ARTCC legacy rules
                            string? effectiveParent =
                                childToParent.TryGetValue(
                                    (artcc.ToUpperInvariant(),
                                     sectorNum.ToUpperInvariant()),
                                    out var p1) ? p1 :
                                childToParent.TryGetValue(
                                    ("", sectorNum.ToUpperInvariant()),
                                    out var p2) ? p2 : null;

                            if (effectiveParent != null || isParent)
                            {
                                string parentSector = effectiveParent ?? sectorNum;
                                var combinedKey =
                                    $"{artcc}-{parentSector}+".ToUpperInvariant();
                                if (!sectorResult.ContainsKey(combinedKey))
                                    sectorResult[combinedKey] = new int[colCount];
                                sectorResult[combinedKey][col]++;
                            }

                            // ARTCC count
                            if (artccResult.ContainsKey(artcc))
                                artccResult[artcc][col]++;

                            // Detail map
                            int artccIdx = artccList.FindIndex(a =>
                                a.Identifier.Equals(artcc,
                                    StringComparison.OrdinalIgnoreCase));
                            if (artccIdx >= 0)
                            {
                                var key = (artccIdx, col);
                                if (!detailMap.ContainsKey(key))
                                    detailMap[key] = new();
                                detailMap[key].Add((pilot, pos));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"NasMonitor: error on {pilot.Callsign} — " +
                            $"{ex.GetType().Name}: {ex.Message}");
                    }
                }

                // TRACON counts
                var traconResult = new Dictionary<string, int[]>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (var config in monitoredTracons)
                {
                    traconResult[config.Identifier] = new int[colCount];
                    var boundaries = _mapDataService.FindTracons(config.Identifier);
                    var allRings = boundaries.SelectMany(b => b.Rings).ToList();

                    foreach (var pilot in pilots)
                    {
                        if (pilot.GroundSpeed <= 35) continue;

                        try
                        {
                            for (int col = 0; col < colCount; col++)
                            {
                                LatLon? pos;
                                int estimatedAlt;

                                if (minutes[col] == 0)
                                {
                                    pos = new LatLon(pilot.Lat, pilot.Lon);
                                    estimatedAlt = pilot.Altitude;
                                }
                                else
                                {
                                    pos = RouteProjector.ProjectPosition(
                                        pilot, minutes[col]);
                                    if (pos == null) continue;
                                    estimatedAlt = _mapDataService.EstimateAltitude(
                                        pilot, pos);
                                }

                                if (estimatedAlt > config.AltitudeCeiling) continue;

                                bool inside = allRings.Any(ring =>
                                    _mapDataService.IsPointInPolygon(
                                        pos.Lat, pos.Lon, ring));

                                if (inside)
                                    traconResult[config.Identifier][col]++;
                            }
                        }
                        catch { }
                    }
                }

                return (artccResult, sectorResult, detailMap, traconResult);
            });

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (Rows.Count > 0 && Rows[0].Cells.Count != colCount)
            {
                InitializeRows();
                BuildTimeLabels();
            }

            _cellDetails = details;
            SectorCounts = sectorCounts;
            ArtccCounts = artccCounts;
            TraconCounts = traconCounts;

            for (int a = 0; a < Rows.Count && a < artccList.Count; a++)
            {
                var counts = artccCounts.TryGetValue(
                    artccList[a].Identifier, out var c) ? c : null;

                for (int col = 0; col < colCount &&
                    col < Rows[a].Cells.Count; col++)
                {
                    Rows[a].Cells[col].Count = counts?[col] ?? 0;
                }
            }

            OnPropertyChanged(nameof(Rows));
            OnPropertyChanged(nameof(SectorCounts));
            OnPropertyChanged(nameof(ArtccCounts));
            OnPropertyChanged(nameof(TraconCounts));
        });
    }

    [RelayCommand]
    private void AddTracon()
    {
        var id = TraconIdentifier.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(id)) return;

        if (MonitoredTracons.Any(t =>
            t.Identifier.Equals(id, StringComparison.OrdinalIgnoreCase)))
            return;

        var tracons = _mapDataService.FindTracons(id);
        if (tracons.Count == 0) return;

        MonitoredTracons.Add(new TraconMonitorConfig
        {
            Identifier = id,
            AltitudeCeiling = TraconAltitude
        });

        TraconIdentifier = string.Empty;
        if (IsEnabled) _ = RecalculateAsync();
    }

    [RelayCommand]
    private void RemoveTracon(TraconMonitorConfig config)
    {
        MonitoredTracons.Remove(config);
        if (IsEnabled) _ = RecalculateAsync();
    }

    [RelayCommand]
    private void Save()
    {
        if (_lastSavePath != null)
            DoSave(_lastSavePath);
        else
            _ = SaveAsAsync();
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var window = Avalonia.Application.Current
            ?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes
            .IClassicDesktopStyleApplicationLifetime;
        if (window?.MainWindow == null) return;

        var file = await window.MainWindow.StorageProvider
            .SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Save Monitor Settings",
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(
                            "Monitor Settings")
                        {
                            Patterns = new[] { "*.json" }
                        }
                    }
                });

        if (file != null)
        {
            _lastSavePath = file.Path.LocalPath;
            DoSave(_lastSavePath);
        }
    }

    private void DoSave(string path)
    {
        var settings = new NasMonitorSettings
        {
            Thresholds = _thresholds.Values.ToList(),
            CombineRules = CombineRules.ToList(),
            MonitoredTracons = MonitoredTracons.ToList()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(settings,
            new System.Text.Json.JsonSerializerOptions
            { WriteIndented = true });

        System.IO.File.WriteAllText(path, json);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var window = Avalonia.Application.Current
            ?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes
            .IClassicDesktopStyleApplicationLifetime;
        if (window?.MainWindow == null) return;

        var files = await window.MainWindow.StorageProvider
            .OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Load Monitor Settings",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(
                            "Monitor Settings")
                        {
                            Patterns = new[] { "*.json" }
                        }
                    }
                });

        if (files.Count > 0)
        {
            var json = System.IO.File.ReadAllText(
                files[0].Path.LocalPath);
            var settings = System.Text.Json.JsonSerializer
                .Deserialize<NasMonitorSettings>(json);

            if (settings != null)
            {
                _thresholds = settings.Thresholds
                    .ToDictionary(t => t.Identifier);
                CombineRules = settings.CombineRules ?? new();

                MonitoredTracons.Clear();
                foreach (var tc in settings.MonitoredTracons ?? new())
                    MonitoredTracons.Add(tc);

                OnPropertyChanged(nameof(Rows));
                OnPropertyChanged(nameof(SectorCounts));

                // Normalize all loaded combine rules to match stripped sector numbers
                CombineRules = CombineRules.Select(r => new SectorCombineRule
                {
                    Artcc = r.Artcc,
                    Parent = r.Parent.TrimStart('0').PadLeft(1, '0'),
                    Children = r.Children
                        .Select(c => c.TrimStart('0').PadLeft(1, '0'))
                        .ToList()
                }).ToList();

                // Trigger recalculate to show updated counts
                if (IsEnabled)
                    _ = RecalculateAsync();
                else
                    OnPropertyChanged(nameof(SectorCounts));
            }
        }
    }
}