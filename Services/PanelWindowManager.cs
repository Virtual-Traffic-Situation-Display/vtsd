using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using vTFMS.Views.Panels;

namespace vTFMS.Services;

public class PanelWindowManager : IPanelWindowManager
{
    private readonly Dictionary<Type, BasePanelWindow> _openPanels = new();
    private readonly Window _owner;

    public PanelWindowManager(Window owner)
    {
        _owner = owner;
    }

    public void Open<TWindow>() where TWindow : BasePanelWindow, new()
    {
        var type = typeof(TWindow);

        // If already open, bring it to front instead of opening a second one
        if (_openPanels.TryGetValue(type, out var existing))
        {
            existing.Activate();
            return;
        }

        var window = new TWindow();
        window.Show(_owner);

        _openPanels[type] = window;

        // Remove from tracking when closed
        window.Closed += (_, _) => _openPanels.Remove(type);
    }

    public void CloseAll()
    {
        foreach (var panel in _openPanels.Values.ToList())
            panel.Close();
    }

    public bool IsOpen<TWindow>() where TWindow : BasePanelWindow
    {
        return _openPanels.ContainsKey(typeof(TWindow));
    }

    public void OpenWithArgs(BasePanelWindow window)
    {
        var type = window.GetType();

        if (_openPanels.TryGetValue(type, out var existing))
        {
            existing.Activate();
            window.Close();
            return;
        }

        window.Show(_owner);
        _openPanels[type] = window;
        window.Closed += (_, _) => _openPanels.Remove(type);
    }
}