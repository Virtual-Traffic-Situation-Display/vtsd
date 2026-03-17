using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class RangeRingsPanelWindow : BasePanelWindow
{
    public RangeRingsPanelWindow()
    {
        DataContext = new RangeRingsPanelViewModel();
    }
}