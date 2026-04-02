using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class FindFlightPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    [ObservableProperty]
    private string _searchPattern = string.Empty;

    [ObservableProperty]
    private string _highlightColor = "#FFFF00";

    [ObservableProperty]
    private string? _selectedCallsign;

    public ObservableCollection<string> FoundCallsigns { get; } = new();

    public string FoundCountText => FoundCallsigns.Count switch
    {
        0 => "No flights found",
        1 => "1 flight found",
        _ => $"{FoundCallsigns.Count} flights found"
    };

    public FindFlightPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "FIND FLIGHT";
        _tsdViewModel = tsdViewModel;
    }

    // =========================================================================
    // Search
    // =========================================================================

    [RelayCommand]
    private void Find()
    {
        if (string.IsNullOrWhiteSpace(SearchPattern)) return;

        var pattern = SearchPattern.Trim();
        var regex = WildcardToRegex(pattern);

        var allPilots = _tsdViewModel.AllCurrentPilots;

        foreach (var pilot in allPilots)
        {
            if (FoundCallsigns.Contains(pilot.Callsign)) continue;

            if (regex.IsMatch(pilot.Callsign))
            {
                FoundCallsigns.Add(pilot.Callsign);
            }
        }

        OnPropertyChanged(nameof(FoundCountText));
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedCallsign == null) return;
        FoundCallsigns.Remove(SelectedCallsign);
        SelectedCallsign = null;
        OnPropertyChanged(nameof(FoundCountText));
    }

    [RelayCommand]
    private void ClearAll()
    {
        FoundCallsigns.Clear();
        OnPropertyChanged(nameof(FoundCountText));
    }

    // =========================================================================
    // Wildcard matching
    // =========================================================================

    /// <summary>
    /// Converts a wildcard pattern (* = any string, ? = any single char)
    /// to a compiled Regex for case-insensitive matching.
    /// </summary>
    private static Regex WildcardToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern);
        // Regex.Escape turns * into \* and ? into \? — undo those
        escaped = escaped.Replace("\\*", ".*").Replace("\\?", ".");
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase);
    }

    // =========================================================================
    // Apply / OK / Cancel
    // =========================================================================

    [RelayCommand]
    private void Apply()
    {
        var callsignSet = new HashSet<string>(
            FoundCallsigns, StringComparer.OrdinalIgnoreCase);
        _tsdViewModel.UpdateFoundFlights(callsignSet, HighlightColor);
    }

    [RelayCommand]
    private void Ok()
    {
        Apply();
        OkRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        OkRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OkRequested;
}