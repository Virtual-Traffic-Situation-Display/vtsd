using System;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class OverlaysPanelWindow : BasePanelWindow
{
    public OverlaysPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public OverlaysPanelWindow(TsdViewModel tsdViewModel)
    {
        var vm = new OverlaysPanelViewModel(tsdViewModel);
        DataContext = vm;
        InitializeComponent();

        vm.OkRequested += (_, _) => Close();
    }
}