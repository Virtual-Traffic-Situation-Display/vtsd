using vTFMS.Views.Panels;

namespace vTFMS.Services;

public interface IPanelWindowManager
{
    void Open<TWindow>() where TWindow : BasePanelWindow, new();
    void OpenWithArgs(BasePanelWindow window);
    void CloseAll();
    bool IsOpen<TWindow>() where TWindow : BasePanelWindow;
}