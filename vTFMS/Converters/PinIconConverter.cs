using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace vTFMS.Converters;

public class PinIconConverter : IValueConverter
{
    public static readonly PinIconConverter Instance = new();

    public object Convert(object? value, Type targetType,
                          object? parameter, CultureInfo culture)
    {
        // Pinned = filled thumbtack, unpinned = outline/rotated
        return value is true ? "📌" : "📍";
    }

    public object ConvertBack(object? value, Type targetType,
                              object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}