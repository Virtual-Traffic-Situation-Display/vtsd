using System;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class RangeRingsPanelWindow : BasePanelWindow
{
    public RangeRingsPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public RangeRingsPanelWindow(TsdViewModel tsdViewModel)
    {
        DataContext = new RangeRingsPanelViewModel(tsdViewModel);
        InitializeComponent();
    }
}