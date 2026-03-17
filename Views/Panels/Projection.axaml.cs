using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class ProjectionPanelWindow : BasePanelWindow
{
    public ProjectionPanelWindow()
    {
        DataContext = new ProjectionPanelViewModel();
    }
}