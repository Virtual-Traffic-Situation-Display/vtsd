using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class OverlaysPanelWindow : BasePanelWindow
{
    public OverlaysPanelWindow()
    {
        DataContext = new OverlaysPanelViewModel();
    }
}