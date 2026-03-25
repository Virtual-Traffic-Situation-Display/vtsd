using System.Collections.Generic;
using System.Linq;

namespace vTFMS.Models;

public class VatsimPilot
{
    public string Callsign { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int Altitude { get; set; }
    public int GroundSpeed { get; set; }
    public int Heading { get; set; }
    public string AircraftType { get; set; } = string.Empty;
    public string Departure { get; set; } = string.Empty;
    public string Arrival { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string MatchedFilterColor { get; set; } = "#FFFFFF";
    public bool MatchedDrawRoute { get; set; } = false;
    public bool MatchedShowRoute { get; set; } = false;
    
    // Per-pilot overrides (set via right-click context menu)
    public bool ShowDataBlock { get; set; } = false;
    public bool ShowOrgDest { get; set; } = false;
    public bool ManualDrawRoute { get; set; } = false;
    public bool ManualShowRoute { get; set; } = false;
    public string? ColorOverride { get; set; }
    public bool IsHidden { get; set; } = false;
    public List<LatLon> ParsedRoute { get; set; } = new();

    // Speed averaging for smoother projections
    private readonly Queue<int> _speedHistory = new();
    private const int MaxSpeedSamples = 5;

    public int AverageSpeed => _speedHistory.Count > 0
        ? (int)_speedHistory.Average()
        : GroundSpeed;

    public void RecordSpeed(int speed)
    {
        // Reject abrupt outliers (>50% change from average)
        if (_speedHistory.Count >= 2)
        {
            double avg = _speedHistory.Average();
            if (avg > 0 && System.Math.Abs(speed - avg) > avg * 0.5)
            {
                // Dampen: use midpoint between outlier and average
                speed = (int)((speed + avg) / 2);
            }
        }

        _speedHistory.Enqueue(speed);
        if (_speedHistory.Count > MaxSpeedSamples)
            _speedHistory.Dequeue();
    }
}