using Avalonia.Controls;
using Avalonia.Input;
using System;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class BasePanelWindow : Window
{
    public BasePanelWindow()
    {
        InitializeComponent();
    }

    public new object? Content
    {
        get => PanelContent?.Content;
        set
        {
            // Don't intercept if it's the base Panel being set internally
            if (value is Panel)
            {
                base.Content = value;
                return;
            }
            if (PanelContent != null)
                PanelContent.Content = value;
            else
                Initialized += (_, _) =>
                {
                    if (PanelContent != null)
                        PanelContent.Content = value;
                };
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is BasePanelViewModel vm)
        {
            vm.PinStateChanged += (_, isPinned) =>
                Topmost = isPinned;
            Topmost = vm.IsPinned;
        }
    }

    private void TitleBar_PointerPressed(object? sender,
        PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void CloseButton_Click(object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}