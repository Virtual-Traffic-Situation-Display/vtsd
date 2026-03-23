using System;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class MoveZoomPanelWindow : BasePanelWindow
{
    public MoveZoomPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public MoveZoomPanelWindow(TsdViewModel tsdViewModel)
    {
        DataContext = new MoveZoomPanelViewModel(tsdViewModel);
        InitializeComponent();
    }
}