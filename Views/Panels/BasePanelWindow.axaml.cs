using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Metadata;
using System;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class BasePanelWindow : Window
{
    /// <summary>
    /// Child AXAML content routes here instead of replacing Window.Content,
    /// so the custom title bar grid stays intact.
    /// </summary>
    public static readonly StyledProperty<object?> PanelBodyProperty =
        AvaloniaProperty.Register<BasePanelWindow, object?>(nameof(PanelBody));

    [Content]
    public object? PanelBody
    {
        get => GetValue(PanelBodyProperty);
        set => SetValue(PanelBodyProperty, value);
    }

    public BasePanelWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is BasePanelViewModel vm)
        {
            vm.PinStateChanged += (_, isPinned) =>
            {
                Topmost = isPinned;
            };
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