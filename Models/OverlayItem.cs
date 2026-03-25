using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace vTFMS.Models;

/// Represents a single overlay row in the Overlays panel.
/// Each item has a display state (Show/Browse/Hide) and
/// an independent label toggle.
public partial class OverlayItem : ObservableObject
{
    /// Display name shown in the panel.
    public string Name { get; init; } = string.Empty;

    /// Key used to map this item back to TsdViewModel properties.
    /// Null for placeholder items with no backing property.
    public string? PropertyKey { get; init; }

    /// Whether this overlay is available (false = disabled placeholder).
    public bool IsEnabled { get; init; } = true;

    [ObservableProperty]
    private OverlayState _state = OverlayState.Hide;

    [ObservableProperty]
    private bool _showLabel = true;

    /// Text displayed on the state button: Show / Hide.
    public string StateText => State == OverlayState.Show ? "Show" : "Hide";

    /// Auto-generated partial method called when State changes.
    /// Notifies the UI that StateText also changed.
    partial void OnStateChanged(OverlayState value)
    {
        OnPropertyChanged(nameof(StateText));
    }

    /// Toggles between Show and Hide.
    [RelayCommand]
    private void CycleState()
    {
        State = State == OverlayState.Show
            ? OverlayState.Hide
            : OverlayState.Show;
    }
}