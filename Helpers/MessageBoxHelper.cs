using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace vTFMS.Helpers;

public static class MessageBoxHelper
{
    public static Task ShowInfoAsync(Window owner, string title, string message)
        => ShowAsync(owner, title, message, confirm: false);

    public static Task<bool> ShowConfirmAsync(Window owner, string title, string message)
        => ShowAsync(owner, title, message, confirm: true);

    private static async Task<bool> ShowAsync(
        Window owner, string title, string message, bool confirm)
    {
        var result = false;

        var dialog = new Window
        {
            Title = title,
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
            Foreground = new SolidColorBrush(Color.Parse("#ccffcc")),
        };

        var text = new TextBlock
        {
            Text = message,
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#ccffcc")),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16, 16, 16, 12)
        };

        var okBtn = new Button
        {
            Content = confirm ? "Yes" : "OK",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            MinWidth = 70,
            Margin = new Thickness(4)
        };
        okBtn.Click += (_, _) => { result = true; dialog.Close(); };

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        btnRow.Children.Add(okBtn);

        if (confirm)
        {
            var cancelBtn = new Button
            {
                Content = "No",
                FontFamily = new FontFamily("Arial"),
                FontSize = 11,
                MinWidth = 70,
                Margin = new Thickness(4)
            };
            cancelBtn.Click += (_, _) => dialog.Close();
            btnRow.Children.Add(cancelBtn);
        }

        var stack = new StackPanel();
        stack.Children.Add(text);
        stack.Children.Add(btnRow);
        dialog.Content = stack;

        await dialog.ShowDialog(owner);
        return result;
    }
}