using System.Diagnostics;
using Avalonia.Controls;

namespace vTFMS.Views;

public partial class AboutWindow : Window
{
    public AboutWindow() : this("dev") { }

    public AboutWindow(string version)
    {
        InitializeComponent();
        VersionText.Text = $"Version {version}";
    }

    private void OnGitHubLinkPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Virtual-Traffic-Situation-Display/vtsd",
            UseShellExecute = true
        });
    }

    private void OnDiscordLinkPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "YOUR_DISCORD_INVITE_LINK_HERE",
            UseShellExecute = true
        });
    }
}