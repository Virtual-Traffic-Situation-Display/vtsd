using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;
using System;

namespace vTFMS.Views.Panels;

public partial class SelectWeatherPanelWindow : BasePanelWindow
{
    public SelectWeatherPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a TsdViewModel.");
    }

    public SelectWeatherPanelWindow(TsdViewModel tsdViewModel)
    {
        DataContext = new SelectWeatherPanelViewModel(tsdViewModel);
        InitializeComponent();

        // Inform TsdViewModel of screen dimensions for radar bounds
        var screen = Screens.Primary;
        if (screen != null)
        {
            tsdViewModel.UpdateScreenSize(
                screen.Bounds.Width,
                screen.Bounds.Height);
        }
    }
}