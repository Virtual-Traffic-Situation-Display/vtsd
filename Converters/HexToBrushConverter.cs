using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace vTFMS.Converters;

public class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType,
                          object? parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try { return new SolidColorBrush(Color.Parse(hex)); }
            catch { }
        }
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object? value, Type targetType,
                              object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}