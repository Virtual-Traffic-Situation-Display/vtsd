using System;
using Avalonia;
using Velopack;

namespace vTFMS
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // MUST be the first thing that runs — Velopack uses this to
            // apply pending updates before Avalonia initializes.
            VelopackApp.Build().Run();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}