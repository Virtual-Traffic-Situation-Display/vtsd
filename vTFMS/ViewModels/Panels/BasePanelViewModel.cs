using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Runtime.CompilerServices;

namespace vTFMS.ViewModels.Panels;

public partial class BasePanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Panel";

    [ObservableProperty]
    private bool _isPinned = false;

    partial void OnIsPinnedChanged(bool value)
    {
        PinStateChanged?.Invoke(this, value);
    }

    public event EventHandler<bool>? PinStateChanged;

    [RelayCommand]
    private void TogglePin()
    {
        IsPinned = !IsPinned;
    }
}