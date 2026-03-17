using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using vTFMS.Models;

namespace vTFMS.ViewModels.Panels;

public partial class ShowMapItemPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private MapItem? _selectedItem;

    public ObservableCollection<MapItem> ActiveMapItems =>
        _tsdViewModel.ActiveMapItems;

    public ShowMapItemPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Show Map Item";
        _tsdViewModel = tsdViewModel;
    }

    [RelayCommand]
    private void AddItems()
    {
        var (found, message) = _tsdViewModel.TryAddMapItems(InputText);
        StatusMessage = message;
        if (found) InputText = string.Empty;
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem != null)
            _tsdViewModel.RemoveMapItem(SelectedItem);
    }
}