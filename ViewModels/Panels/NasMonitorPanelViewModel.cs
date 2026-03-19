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
    public int Count { get; set; } = -1; // -1 = disabled
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

    // Manual property — logic only fires when explicitly set (on mouse release),
    // not on every slider tick
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

        // Subscribe to VATSIM data refresh (not filtered pilots)
        _onPilotsRefreshed = (_, _) => { if (IsEnabled) _ = RecalculateAsync(); };
        _tsdViewModel.PilotsRefreshed += _onPilotsRefreshed;

        InitializeRows();
        BuildTimeLabels();
    }

    public void Dispose()
    {
        _tsdViewModel.PilotsRefreshed -= _onPilotsRefreshed;
    }

    private void InitializeRows()
    {
        Rows.Clear();
        foreach (var artcc in _tsdViewModel.ArtccBoundaries)
        {
            var row = new NasMonitorRow
            {
                CenterId = artcc.Identifier
            };
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

        int count = 1; // Now
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
                cell.Count = -1;
        OnPropertyChanged(nameof(Rows));
    }

    public async Task RecalculateAsync()
    {
        if (!IsEnabled) return;

        // Always rebuild time labels so "Now" stays current
        BuildTimeLabels();

        // Rebuild rows only if column count changed
        var newColCount = GetColumnCount();
        if (newColCount != Rows.FirstOrDefault()?.Cells.Count)
            InitializeRows();

        // Snapshot — VatsimService can replace CurrentPilots on another thread
        var pilots = _tsdViewModel.AllCurrentPilots.ToList();

        await _tsdViewModel.ResolveAllRoutesAsync(pilots);

        var artccList = _tsdViewModel.ArtccBoundaries.ToList();

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

        var counts = await Task.Run(() =>
        {
            var result = new int[artccList.Count, colCount];

            foreach (var pilot in pilots)
            {
                if (pilot.GroundSpeed <= 35) continue;

                try
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        LatLon? pos;
                        if (minutes[col] == 0)
                            pos = new LatLon(pilot.Lat, pilot.Lon);
                        else
                            pos = RouteProjector.ProjectPosition(
                                pilot, minutes[col]);

                        if (pos == null) continue;

                        for (int a = 0; a < artccList.Count; a++)
                        {
                            if (_mapDataService.IsPointInArtcc(
                                pos.Lat, pos.Lon, artccList[a]))
                            {
                                result[a, col]++;
                            }
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

            return result;
        });

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (Rows.Count > 0 && Rows[0].Cells.Count != colCount)
            {
                InitializeRows();
                BuildTimeLabels();
            }

            for (int a = 0; a < Rows.Count && a < artccList.Count; a++)
            {
                for (int col = 0; col < colCount && col < Rows[a].Cells.Count; col++)
                {
                    Rows[a].Cells[col].Count = counts[a, col];
                }
            }

            OnPropertyChanged(nameof(Rows));
        });
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
            HorizonMinutes = HorizonMinutes,
            Thresholds = _thresholds.Values.ToList()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(settings,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

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
                HorizonMinutes = settings.HorizonMinutes;
                OnPropertyChanged(nameof(Rows));
            }
        }
    }
}