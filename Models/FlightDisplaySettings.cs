namespace vTFMS.Models;

/// <summary>
/// Global flight display settings controlled by the
/// Flights → Customize → Flight Display dialog.
/// Per-pilot overrides (via right-click) take priority.
/// </summary>
public class FlightDisplaySettings
{
    // Show section — data block content
    public bool ShowDataBlocks { get; set; } = false;
    public bool ShowOrgDest { get; set; } = false;
    public bool ShowRouteText { get; set; } = false;

    // Draw section — route and projection lines
    public bool DrawRoutes { get; set; } = false;
    public bool ShowLastTz { get; set; } = false;
    public bool ShowLeadLines { get; set; } = false;
    public int LeadLineMinutes { get; set; } = 5;

    // History section
    public bool CollectHistory { get; set; } = false;
    public int HistoryIntervalMinutes { get; set; } = 5;
    public bool DrawHistory { get; set; } = false;

    public FlightDisplaySettings Clone() => new()
    {
        ShowDataBlocks = ShowDataBlocks,
        ShowOrgDest = ShowOrgDest,
        ShowRouteText = ShowRouteText,
        DrawRoutes = DrawRoutes,
        ShowLastTz = ShowLastTz,
        ShowLeadLines = ShowLeadLines,
        LeadLineMinutes = LeadLineMinutes,
        CollectHistory = CollectHistory,
        HistoryIntervalMinutes = HistoryIntervalMinutes,
        DrawHistory = DrawHistory
    };
}