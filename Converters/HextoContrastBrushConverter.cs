using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace vTFMS.Converters;

public class HexToContrastBrushConverter : IValueConverter
{
    public static readonly HexToContrastBrushConverter Instance = new();

    public object Convert(object? value, Type targetType,
                          object? parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try
            {
                var c = Color.Parse(hex);
                double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                return luminance > 128 ? Brushes.Black : Brushes.White;
            }
            catch { }
        }
        return Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType,
                              object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}