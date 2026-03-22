using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace vTFMS.Services;

public class FontService : IFontService
{
    public List<string> GetMonospaceFonts()
    {
        // Symbol/dingbat fonts that pass monospace detection
        var excludedFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Marlett", "Wingdings", "Wingdings 2", "Wingdings 3",
            "Symbol", "Webdings", "MT Extra", "HoloLens MDL2 Assets",
            "Segoe MDL2 Assets", "Segoe Fluent Icons"
        };
    
        var mono = new List<string>();
    
        foreach (var family in FontManager.Current.SystemFonts)
        {
            try
            {
                if (excludedFonts.Contains(family.Name))
                    continue;
    
                if (IsMonospace(family))
                    mono.Add(family.Name);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine(
                    $"FontService: skipping {family.Name}");
            }
        }
    
        if (!mono.Contains("Courier New"))
            mono.Add("Courier New");
    
        mono.Sort(StringComparer.OrdinalIgnoreCase);
        return mono;
    }

    private static bool IsMonospace(FontFamily family)
    {
        var typeface = new Typeface(family);

        var narrowChar = new FormattedText(
            "i", CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, typeface,
            12, Brushes.Black);

        var wideChar = new FormattedText(
            "W", CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, typeface,
            12, Brushes.Black);

        return Math.Abs(narrowChar.Width - wideChar.Width) < 0.01;
    }
}