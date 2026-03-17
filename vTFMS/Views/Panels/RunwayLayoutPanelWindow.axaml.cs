using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class RunwayLayoutPanelWindow : BasePanelWindow
{
    public RunwayLayoutPanelWindow()
    {
        DataContext = new RunwayLayoutPanelViewModel();
    }
}