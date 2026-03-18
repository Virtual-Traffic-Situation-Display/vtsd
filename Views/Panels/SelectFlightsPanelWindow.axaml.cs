using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using vTFMS.Models;
using vTFMS.ViewModels;
using vTFMS.ViewModels.Panels;

namespace vTFMS.Views.Panels;

public partial class SelectFlightsPanelWindow : BasePanelWindow
{
    public SelectFlightsPanelWindow()
    {
        throw new InvalidOperationException(
            "Use the constructor that takes a SelectFlightsPanelViewModel.");
    }

    public SelectFlightsPanelWindow(SelectFlightsPanelViewModel vm)
    {
        vm.OkRequested += (_, _) => Close();
        DataContext = vm;
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        var vm = (SelectFlightsPanelViewModel)DataContext!;

        // Column headers
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("30,50,60,80,80,70,60,70,70,50"),
            Margin = new Thickness(4, 4, 4, 0)
        };

        var headers = new[]
        {
            "", "Show", "Color", "Arrival", "Departure",
            "Data Block", "Show Route", "Draw Route", "Filter"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var h = new TextBlock
            {
                Text = headers[i],
                FontFamily = new FontFamily("Arial"),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(h, i);
            headerGrid.Children.Add(h);
        }

        // Scrollable rows
        var itemsControl = new ItemsControl
        {
            ItemsSource = vm.Filters,
            ItemTemplate = new FuncDataTemplate<FlightFilter>((filter, _) =>
                BuildFilterRow(filter), true)
        };

        var scrollViewer = new ScrollViewer
        {
            Content = itemsControl,
            VerticalScrollBarVisibility =
                Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        // Bottom buttons
        var addRowBtn = new Button
        {
            Content = "+ Add Row",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Margin = new Thickness(4)
        };
        addRowBtn.Bind(Button.CommandProperty,
            new Binding(nameof(vm.AddRowCommand)));

        var applyBtn = new Button
        {
            Content = "Apply",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Margin = new Thickness(4)
        };
        applyBtn.Bind(Button.CommandProperty,
            new Binding(nameof(vm.ApplyCommand)));

        var okBtn = new Button
        {
            Content = "OK",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Margin = new Thickness(4)
        };
        okBtn.Bind(Button.CommandProperty,
            new Binding(nameof(vm.OkCommand)));

        var cancelBtn = new Button
        {
            Content = "Cancel",
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Margin = new Thickness(4)
        };
        cancelBtn.Bind(Button.CommandProperty,
            new Binding(nameof(vm.CancelCommand)));

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(4)
        };
        buttonRow.Children.Add(addRowBtn);
        buttonRow.Children.Add(okBtn);
        buttonRow.Children.Add(applyBtn);
        buttonRow.Children.Add(cancelBtn);

        // Main layout
        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(4)
        };

        Grid.SetRow(headerGrid, 0);
        Grid.SetRow(scrollViewer, 1);
        Grid.SetRow(buttonRow, 2);

        mainGrid.Children.Add(headerGrid);
        mainGrid.Children.Add(scrollViewer);
        mainGrid.Children.Add(buttonRow);

        PanelContent.Content = mainGrid;
    }

    private Control BuildFilterRow(FlightFilter filter)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("30,50,60,80,80,70,60,70,70,50"),
            Margin = new Thickness(4, 2, 4, 2)
        };

        // Row number
        var rowNum = new TextBlock
        {
            Text = filter.RowNumber.ToString(),
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(rowNum, 0);

        // Show checkbox
        var showCheck = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        showCheck.Bind(CheckBox.IsCheckedProperty,
            new Binding(nameof(filter.Show))
            { Mode = BindingMode.TwoWay });
        Grid.SetColumn(showCheck, 1);

        // Color swatch — clickable rectangle
        // Color swatch — opens color picker popup
        var colorRect = new Border
        {
            Width = 40,
            Height = 20,
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse(filter.Color)),
            Cursor = new Cursor(StandardCursorType.Hand),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#545454"))
        };

        colorRect.PointerPressed += (_, e) =>
        {
            var picker = new ColorPickerWindow();
            picker.ColorSelected += (_, hex) =>
            {
                filter.Color = hex;
                colorRect.Background = new SolidColorBrush(Color.Parse(hex));
            };
            picker.Show(this);
        };

        Grid.SetColumn(colorRect, 2);

        // Arrival text box
        var arrivalBox = new TextBox
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        arrivalBox.Bind(TextBox.TextProperty,
            new Binding(nameof(filter.Arrival))
            { Mode = BindingMode.TwoWay });
        Grid.SetColumn(arrivalBox, 3);

        // Departure text box
        var departureBox = new TextBox
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 11,
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        departureBox.Bind(TextBox.TextProperty,
            new Binding(nameof(filter.Departure))
            { Mode = BindingMode.TwoWay });
        Grid.SetColumn(departureBox, 4);

        // Data Block checkbox
        var dataBlockCheck = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        dataBlockCheck.Bind(CheckBox.IsCheckedProperty,
            new Binding(nameof(filter.DataBlock))
            { Mode = BindingMode.TwoWay });
        Grid.SetColumn(dataBlockCheck, 5);

        // Show Route checkbox
        var showRouteCheck = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        showRouteCheck.Bind(CheckBox.IsCheckedProperty,
            new Binding(nameof(filter.ShowRoute))
            { Mode = BindingMode.TwoWay });
        Grid.SetColumn(showRouteCheck, 6);

        // Draw Route checkbox
        var drawRouteCheck = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        drawRouteCheck.Bind(CheckBox.IsCheckedProperty,
            new Binding(nameof(filter.DrawRoute))
            { Mode = BindingMode.TwoWay });
        Grid.SetColumn(drawRouteCheck, 7);

        // Filter checkbox
        var filterCheck = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        filterCheck.Bind(CheckBox.IsCheckedProperty,
            new Binding(nameof(filter.Filter))
            { Mode = BindingMode.TwoWay });
        Grid.SetColumn(filterCheck, 8);

        grid.Children.Add(rowNum);
        grid.Children.Add(showCheck);
        grid.Children.Add(colorRect);
        grid.Children.Add(arrivalBox);
        grid.Children.Add(departureBox);
        grid.Children.Add(dataBlockCheck);
        grid.Children.Add(showRouteCheck);
        grid.Children.Add(drawRouteCheck);
        grid.Children.Add(filterCheck);

        return grid;
    }
}