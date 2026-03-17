using CommunityToolkit.Mvvm.ComponentModel;

namespace vTFMS.Models;

public partial class FlightFilter : ObservableObject
{
    [ObservableProperty]
    private bool _show = true;

    [ObservableProperty]
    private string _color = "#0000FF";

    [ObservableProperty]
    private string _arrival = string.Empty;

    [ObservableProperty]
    private string _departure = string.Empty;

    [ObservableProperty]
    private bool _dataBlock = false;

    [ObservableProperty]
    private bool _showRoute = true;

    [ObservableProperty]
    private bool _drawRoute = false;

    [ObservableProperty]
    private bool _filter = false;

    public int RowNumber { get; set; }
}