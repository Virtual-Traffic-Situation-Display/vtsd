using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace vTFMS.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        return value is true ? 1.0 : 0.3;
    }

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToVatsimTipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        return value is true ? "VATSIM data active" : "VATSIM data disabled";
    }

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}