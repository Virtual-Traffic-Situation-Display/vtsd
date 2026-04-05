using vTFMS.Models;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class FlightDetailWindow : BasePanelWindow
{
    private readonly FlightDetailPanelViewModel _vm;

    public FlightDetailWindow()
    {
        _vm = new FlightDetailPanelViewModel();
        DataContext = _vm;
        InitializeComponent();
    }

    public FlightDetailWindow(VatsimPilot pilot) : this()
    {
        _vm.LoadPilot(pilot);
    }

    public void UpdatePilot(VatsimPilot pilot)
    {
        _vm.LoadPilot(pilot);
    }
}
