using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public class FlightCountRow
{
    public string Arr { get; set; } = string.Empty;
    public string Dep { get; set; } = string.Empty;
    public int Active { get; set; }
    public int Visible { get; set; }
}

public partial class FlightCountPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    public ObservableCollection<FlightCountRow> Rows { get; } = new();

    public FlightCountPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Flight Count";
        _tsdViewModel = tsdViewModel;
        Refresh();

        // Refresh when pilots update
        _tsdViewModel.VisiblePilots.CollectionChanged +=
            (_, _) => Refresh();
    }

    public void Refresh()
    {
        System.Diagnostics.Debug.WriteLine(
            $"FlightCount: allPilots={_tsdViewModel.AllCurrentPilots.Count}, " +
            $"visible={_tsdViewModel.VisiblePilots.Count}");

        Rows.Clear();

        var allPilots = _tsdViewModel.AllCurrentPilots;
        var visiblePilots = _tsdViewModel.VisiblePilots.ToList();
        var filters = _tsdViewModel.SelectFlightsViewModel
        .Filters
        .Where(f => f.Show &&
            (!string.IsNullOrWhiteSpace(f.Arrival) ||
             !string.IsNullOrWhiteSpace(f.Departure)))
        .ToList();

        // ALL row
        Rows.Add(new FlightCountRow
        {
            Arr = "ALL",
            Dep = "ALL",
            Active = CountMatching(allPilots, null),
            Visible = CountMatching(visiblePilots, null)
        });

        System.Diagnostics.Debug.WriteLine(
            $"FlightCount: Rows.Count={Rows.Count}, " +
            $"ALL Active={Rows[0].Active}");

        // One row per active filter
        foreach (var filter in filters)
        {
            Rows.Add(new FlightCountRow
            {
                Arr = string.IsNullOrWhiteSpace(filter.Arrival)
                    ? "ALL" : filter.Arrival.ToUpperInvariant(),
                Dep = string.IsNullOrWhiteSpace(filter.Departure)
                    ? "ALL" : filter.Departure.ToUpperInvariant(),
                Active = CountMatching(allPilots, filter),
                Visible = CountMatching(visiblePilots, filter)
            });
        }
    }

    private static int CountMatching(
        IEnumerable<VatsimPilot> pilots,
        FlightFilter? filter)
    {
        if (filter == null)
            return pilots.Count();

        return pilots.Count(p =>
        {
            bool arrivalMatch =
                string.IsNullOrWhiteSpace(filter.Arrival) ||
                filter.Arrival
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(a => p.Arrival.Contains(
                        a.ToUpperInvariant(),
                        StringComparison.OrdinalIgnoreCase));

            bool departureMatch =
                string.IsNullOrWhiteSpace(filter.Departure) ||
                filter.Departure
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(d => p.Departure.Contains(
                        d.ToUpperInvariant(),
                        StringComparison.OrdinalIgnoreCase));

            return arrivalMatch && departureMatch;
        });
    }
}