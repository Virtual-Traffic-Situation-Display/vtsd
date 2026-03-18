using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class OverlaysPanelWindow : BasePanelWindow
{
    private readonly TsdViewModel _tsdViewModel;

    public OverlaysPanelWindow(TsdViewModel tsdViewModel)
    {
        _tsdViewModel = tsdViewModel;

        var vm = new BasePanelViewModel { Title = "OVERLAYS" };
        DataContext = vm;
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        var artccCheck = new CheckBox
        {
            Content = "Show ARTCC Boundaries",
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            Margin = new Avalonia.Thickness(8)
        };
        artccCheck.Bind(CheckBox.IsCheckedProperty,
            new Avalonia.Data.Binding("ShowArtcc")
            {
                Source = _tsdViewModel,
                Mode = Avalonia.Data.BindingMode.TwoWay
            });

        var stack = new StackPanel
        {
            Margin = new Avalonia.Thickness(4)
        };
        stack.Children.Add(artccCheck);
        PanelContent.Content = stack;
    }
}