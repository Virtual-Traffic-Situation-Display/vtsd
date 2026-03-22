using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class SelectFlightsPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    public ObservableCollection<FlightFilter> Filters { get; } = new();

    public SelectFlightsPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Select Flights";
        _tsdViewModel = tsdViewModel;

        // Start with one empty row
        AddRow();
    }

    [RelayCommand]
    private void AddRow()
    {
        Filters.Add(new FlightFilter
        {
            RowNumber = Filters.Count + 1,
            Color = GetDefaultColor(Filters.Count)
        });
    }

    [RelayCommand]
    private void RemoveRow(FlightFilter filter)
    {
        Filters.Remove(filter);
        // Renumber remaining rows
        for (int i = 0; i < Filters.Count; i++)
            Filters[i].RowNumber = i + 1;
    }

    [RelayCommand]
    private void Apply()
    {
        _tsdViewModel.SetFlightFilters(Filters.ToList());
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

    private static string GetDefaultColor(int index) => index switch
    {
        0 => "#0000FF",
        1 => "#FF8C00",
        2 => "#FF0000",
        3 => "#FFB6C1",
        4 => "#808080",
        5 => "#90EE90",
        _ => "#FFFFFF"
    };

    public void LoadFilters(List<FlightFilter> filters)
    {
        Filters.Clear();
        int row = 1;
        foreach (var f in filters)
        {
            f.RowNumber = row++;
            Filters.Add(f);
        }
        _tsdViewModel.SetFlightFilters(Filters.ToList());
    }
}