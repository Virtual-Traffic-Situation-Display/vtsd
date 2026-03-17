using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class SelectFeaFcaPanelWindow : BasePanelWindow
{
    public SelectFeaFcaPanelWindow()
    {
        DataContext = new SelectFeaFcaPanelViewModel();
        InitializeComponent();
    }

#pragma warning disable CS0108
    private void InitializeComponent() => base.InitializeComponent();
#pragma warning restore CS0108
}