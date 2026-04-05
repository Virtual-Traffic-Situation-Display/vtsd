using CommunityToolkit.Mvvm.ComponentModel;
using vTFMS.Models;

namespace vTFMS.ViewModels.Panels;

public partial class FlightDetailPanelViewModel : BasePanelViewModel
{
    [ObservableProperty]
    private string _callsign = string.Empty;

    [ObservableProperty]
    private string _pilotName = string.Empty;

    [ObservableProperty]
    private int _cid;

    [ObservableProperty]
    private string _aircraftType = string.Empty;

    [ObservableProperty]
    private string _aircraftFull = string.Empty;

    [ObservableProperty]
    private string _departure = string.Empty;

    [ObservableProperty]
    private string _arrival = string.Empty;

    [ObservableProperty]
    private string _alternate = string.Empty;

    [ObservableProperty]
    private string _flightRules = string.Empty;

    [ObservableProperty]
    private string _route = string.Empty;

    [ObservableProperty]
    private string _currentAltitude = string.Empty;

    [ObservableProperty]
    private string _filedAltitude = string.Empty;

    [ObservableProperty]
    private int _groundSpeed;

    [ObservableProperty]
    private int _heading;

    [ObservableProperty]
    private string _transponder = string.Empty;

    [ObservableProperty]
    private string _assignedTransponder = string.Empty;

    [ObservableProperty]
    private string _cruiseTas = string.Empty;

    [ObservableProperty]
    private string _depTime = string.Empty;

    [ObservableProperty]
    private string _enrouteTime = string.Empty;

    [ObservableProperty]
    private string _fuelTime = string.Empty;

    [ObservableProperty]
    private string _remarks = string.Empty;

    [ObservableProperty]
    private string _position = string.Empty;

    [ObservableProperty]
    private string _server = string.Empty;

    [ObservableProperty]
    private string _logonTime = string.Empty;

    public FlightDetailPanelViewModel()
    {
        Title = "Flight Detail";
    }

    public void LoadPilot(VatsimPilot pilot)
    {
        Title = $"Flight Detail — {pilot.Callsign}";

        Callsign = pilot.Callsign;
        PilotName = pilot.PilotName;
        Cid = pilot.Cid;
        AircraftType = pilot.AircraftType;
        AircraftFull = pilot.AircraftFull;
        Departure = pilot.Departure;
        Arrival = pilot.Arrival;
        Alternate = pilot.Alternate;
        FlightRules = pilot.FlightRules;
        Route = pilot.Route;
        FiledAltitude = FormatFiledAltitude(pilot.FiledAltitude);
        CurrentAltitude = FormatCurrentAltitude(pilot.Altitude);
        GroundSpeed = pilot.GroundSpeed;
        Heading = pilot.Heading;
        Transponder = pilot.Transponder;
        AssignedTransponder = pilot.AssignedTransponder;
        CruiseTas = pilot.CruiseTas;
        DepTime = FormatTime(pilot.DepTime);
        EnrouteTime = FormatDuration(pilot.EnrouteTime);
        FuelTime = FormatDuration(pilot.FuelTime);
        Remarks = pilot.Remarks;
        Position = $"{pilot.Lat:F4}°, {pilot.Lon:F4}°";
        Server = pilot.Server;
        LogonTime = FormatLogonTime(pilot.LogonTime);
    }

    private static string FormatCurrentAltitude(int altitude)
    {
        return altitude >= 18000
            ? $"FL{altitude / 100:000}"
            : $"{altitude:N0} ft";
    }

    private static string FormatFiledAltitude(string filed)
    {
        if (string.IsNullOrWhiteSpace(filed)) return string.Empty;

        // VATSIM often sends the altitude as a plain number string
        if (int.TryParse(filed, out int alt))
        {
            return alt >= 18000
                ? $"FL{alt / 100:000}"
                : $"{alt:N0} ft";
        }

        return filed;
    }

    private static string FormatTime(string hhmm)
    {
        if (string.IsNullOrWhiteSpace(hhmm) || hhmm.Length < 4)
            return hhmm;

        return $"{hhmm[..2]}:{hhmm[2..]}z";
    }

    private static string FormatDuration(string hhmm)
    {
        if (string.IsNullOrWhiteSpace(hhmm) || hhmm.Length < 4)
            return hhmm;

        return $"{hhmm[..2]}h {hhmm[2..]}m";
    }

    private static string FormatLogonTime(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return string.Empty;

        if (System.DateTime.TryParse(iso, null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var dt))
        {
            return dt.ToString("HH:mm:ss") + "z";
        }

        return iso;
    }
}
