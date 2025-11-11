using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine.Fragment;

public static class StyleConverter
{
    public static VisualStyle FromComputed(ComputedStyle s)
    {
        var hasBorders = s.Borders?.HasAny == true;

        return new VisualStyle(
            Borders: hasBorders ? s.Borders : null
        );
    }

    public static FontKey ToFontKey(ComputedStyle s)
    {
        var weight = s.Bold ? FontWeight.W700 : FontWeight.W400;
        var style = s.Italic ? FontStyle.Italic : FontStyle.Normal;
        return new FontKey(s.FontFamily, weight, style);
    }

    public static ColorRgba ParseColor(string hex)
    {
        // Support #RRGGBB or #RRGGBBAA
        if (string.IsNullOrWhiteSpace(hex))
        {
            return new ColorRgba(0, 0, 0, 255);
        }

        hex = hex.TrimStart('#');

        byte r = 0, g = 0, b = 0, a = 255;
        if (hex.Length >= 6)
        {
            r = Convert.ToByte(hex.Substring(0, 2), 16);
            g = Convert.ToByte(hex.Substring(2, 2), 16);
            b = Convert.ToByte(hex.Substring(4, 2), 16);
        }

        if (hex.Length == 8)
        {
            a = Convert.ToByte(hex.Substring(6, 2), 16);
        }

        return new ColorRgba(r, g, b, a);
    }
}
