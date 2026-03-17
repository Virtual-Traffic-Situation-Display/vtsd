using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using vTFMS.Models;
using vTFMS.ViewModels;

namespace vTFMS.ViewModels.Panels;

public partial class AdaptPanelViewModel : BasePanelViewModel
{
    private readonly TsdViewModel _tsdViewModel;
    private DisplaySettings _original = new();

    [ObservableProperty] private string _backgroundColor;
    [ObservableProperty] private string _boundaryColor;
    [ObservableProperty] private string _traconColor;
    [ObservableProperty] private string _artccColor;
    [ObservableProperty] private string _airportColor;
    [ObservableProperty] private string _vorColor;
    [ObservableProperty] private string _ndbColor;
    [ObservableProperty] private string _fixColor;
    [ObservableProperty] private string _jetRoutesColor;
    [ObservableProperty] private string _victorRoutesColor;
    [ObservableProperty] private string _dataBlockFont;
    [ObservableProperty] private double _dataBlockFontSize;
    [ObservableProperty] private string _mapLabelFont;
    [ObservableProperty] private double _mapLabelFontSize;
    [ObservableProperty] private string _dataBlockColor;
    [ObservableProperty] private string _mapLabelColor;

    public AdaptPanelViewModel(TsdViewModel tsdViewModel)
    {
        Title = "Customize Colors and Fonts";
        _tsdViewModel = tsdViewModel;

        LoadFromSettings(_tsdViewModel.DisplaySettings);
        _original = _tsdViewModel.DisplaySettings.Clone();
    }

    private void LoadFromSettings(DisplaySettings s)
    {
        BackgroundColor = s.BackgroundColor;
        BoundaryColor = s.BoundaryColor;
        TraconColor = s.TraconColor;
        ArtccColor = s.ArtccColor;
        AirportColor = s.AirportColor;
        VorColor = s.VorColor;
        NdbColor = s.NdbColor;
        FixColor = s.FixColor;
        JetRoutesColor = s.JetRoutesColor;
        VictorRoutesColor = s.VictorRoutesColor;
        DataBlockFont = s.DataBlockFont;
        DataBlockFontSize = s.DataBlockFontSize;
        MapLabelFont = s.MapLabelFont;
        MapLabelFontSize = s.MapLabelFontSize;
        DataBlockColor = s.DataBlockColor;
        MapLabelColor = s.MapLabelColor;
    }

    private DisplaySettings ToSettings() => new()
    {
        BackgroundColor = BackgroundColor,
        BoundaryColor = BoundaryColor,
        TraconColor = TraconColor,
        ArtccColor = ArtccColor,
        AirportColor = AirportColor,
        VorColor = VorColor,
        NdbColor = NdbColor,
        FixColor = FixColor,
        JetRoutesColor = JetRoutesColor,
        VictorRoutesColor = VictorRoutesColor,
        DataBlockFont = DataBlockFont,
        DataBlockFontSize = DataBlockFontSize,
        MapLabelFont = MapLabelFont,
        MapLabelFontSize = MapLabelFontSize,
        DataBlockColor = DataBlockColor,
        MapLabelColor = MapLabelColor
    };

    [RelayCommand]
    private void Apply()
    {
        _tsdViewModel.DisplaySettings = ToSettings();
        _tsdViewModel.ApplyDisplaySettings();
    }

    [RelayCommand]
    private void Ok()
    {
        Apply();
        OkRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        OkRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Undo()
    {
        LoadFromSettings(_original);
    }

    public event EventHandler? OkRequested;
}