using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using System.Linq;
using vTFMS.Services;
using vTFMS.ViewModels;

namespace vTFMS;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
    
            IMapDataService mapDataService = new MapDataService();
            IVatsimService vatsimService = new VatsimService();
            IProfileService profileService = new ProfileService();
            IWeatherService weatherService = new WeatherService();
    
            desktop.MainWindow = new MainWindow();
    
            var panelManager =
                new PanelWindowManager(desktop.MainWindow);
    
            var mainViewModel = new MainViewModel(panelManager, mapDataService,
                vatsimService, profileService, weatherService,
                desktop.MainWindow);
    
            desktop.MainWindow.DataContext = mainViewModel;
            desktop.Exit += (_, _) => mainViewModel.Dispose();
        }
    
        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators
                .OfType<DataAnnotationsValidationPlugin>()
                .ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}