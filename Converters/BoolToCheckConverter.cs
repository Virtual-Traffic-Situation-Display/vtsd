using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace vTFMS.Converters;

public class BoolToCheckConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        return value is true ? "✓" : null;
    }

    public object? ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}