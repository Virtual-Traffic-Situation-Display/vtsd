using System;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class CustomizeFlightDisplayWindow : BasePanelWindow
{
    public CustomizeFlightDisplayWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public CustomizeFlightDisplayWindow(TsdViewModel tsdViewModel)
    {
        var vm = new CustomizeFlightDisplayViewModel(tsdViewModel);
        DataContext = vm;
        InitializeComponent();

        vm.OkRequested += (_, _) => Close();
    }
}