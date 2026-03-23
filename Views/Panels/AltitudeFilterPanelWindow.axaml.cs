using System;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class AltitudeFilterPanelWindow : BasePanelWindow
{
    public AltitudeFilterPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public AltitudeFilterPanelWindow(TsdViewModel tsdViewModel)
    {
        var vm = new AltitudeFilterPanelViewModel(tsdViewModel);
        vm.OkRequested += (_, _) => Close();
        DataContext = vm;
        InitializeComponent();
    }
}