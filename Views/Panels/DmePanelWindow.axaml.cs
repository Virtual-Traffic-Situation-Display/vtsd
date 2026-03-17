using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class DmePanelWindow : BasePanelWindow
{
    public DmePanelWindow()
    {
        DataContext = new DmePanelViewModel();
    }
}