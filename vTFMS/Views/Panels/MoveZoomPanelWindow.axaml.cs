using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class MoveZoomPanelWindow : BasePanelWindow
{
    public MoveZoomPanelWindow()
    {
        DataContext = new MoveZoomPanelViewModel();
    }
}