using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;
using System;

namespace vTFMS.Views.Panels;

public partial class ShowMapItemPanelWindow : BasePanelWindow
{
    public ShowMapItemPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public ShowMapItemPanelWindow(TsdViewModel tsdViewModel)
    {
        DataContext = new ShowMapItemPanelViewModel(tsdViewModel);
        InitializeComponent();
    }
}