using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class ShowMapItemPanelWindow : BasePanelWindow
{
    public ShowMapItemPanelWindow(TsdViewModel tsdViewModel)
    {
        DataContext = new ShowMapItemPanelViewModel(tsdViewModel);
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        var vm = (ShowMapItemPanelViewModel)DataContext!;

        // Status message
        var statusLabel = new TextBlock
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#000000")),
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };
        statusLabel.Bind(TextBlock.TextProperty,
            new Avalonia.Data.Binding(nameof(vm.StatusMessage)));

        // Input label
        var inputLabel = new TextBlock
        {
            Text = "Enter identifiers (space separated):",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#000000"))
        };

        // Input text box
        var inputBox = new TextBox
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Height = 120,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top
        };
        inputBox.Bind(TextBox.TextProperty,
            new Avalonia.Data.Binding(nameof(vm.InputText))
            {
                Mode = Avalonia.Data.BindingMode.TwoWay
            });

        // Add button
        var addButton = new Button
        {
            Content = "Add Typed Items to List",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        addButton.Bind(Button.CommandProperty,
            new Avalonia.Data.Binding(nameof(vm.AddItemsCommand)));

        // Left panel
        var leftGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,8,Auto,8,*")
        };
        Grid.SetRow(inputLabel, 0);
        Grid.SetRow(inputBox, 2);
        Grid.SetRow(addButton, 4);
        leftGrid.Children.Add(inputLabel);
        leftGrid.Children.Add(inputBox);
        leftGrid.Children.Add(addButton);

        // List label
        var listLabel = new TextBlock
        {
            Text = "Map items to be shown:",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#000000"))
        };

        // List box
        var listBox = new ListBox
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 11
        };
        listBox.Bind(ListBox.ItemsSourceProperty,
            new Avalonia.Data.Binding(nameof(vm.ActiveMapItems)));
        listBox.Bind(ListBox.SelectedItemProperty,
            new Avalonia.Data.Binding(nameof(vm.SelectedItem))
            {
                Mode = Avalonia.Data.BindingMode.TwoWay
            });

        // Remove button
        var removeButton = new Button
        {
            Content = "Remove Selected",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        removeButton.Bind(Button.CommandProperty,
            new Avalonia.Data.Binding(nameof(vm.RemoveSelectedCommand)));

        // Right panel
        var rightGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,8,*,8,Auto")
        };
        Grid.SetRow(listLabel, 0);
        Grid.SetRow(listBox, 2);
        Grid.SetRow(removeButton, 4);
        rightGrid.Children.Add(listLabel);
        rightGrid.Children.Add(listBox);
        rightGrid.Children.Add(removeButton);

        // Main content grid
        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Avalonia.Thickness(8),
            ColumnDefinitions = new ColumnDefinitions("*,8,180")
        };

        Grid.SetColumn(leftGrid, 0);
        Grid.SetColumn(rightGrid, 2);
        Grid.SetRow(leftGrid, 0);
        Grid.SetRow(rightGrid, 0);
        Grid.SetRow(statusLabel, 1);
        Grid.SetColumnSpan(statusLabel, 3);

        mainGrid.Children.Add(leftGrid);
        mainGrid.Children.Add(rightGrid);
        mainGrid.Children.Add(statusLabel);

        // Set into the base panel content slot
        var panelContent = this.FindControl<Control>("PanelContent");
        if (panelContent?.Parent is Panel parent)
        {
            var index = parent.Children.IndexOf(panelContent);
            parent.Children[index] = mainGrid;
        }
        else
        {
            PanelContent.Content = mainGrid;
        }
    }
}