using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public static class StyleConverter
{
    public static VisualStyle FromComputed(ComputedStyle s)
    {
        var hasBorders = s.Borders?.HasAny == true;

        return new VisualStyle(
            BackgroundColor: s.BackgroundColor,
            Borders: hasBorders ? s.Borders : null
        );
    }

    public static FontKey ToFontKey(ComputedStyle s)
    {
        var weight = s.Bold ? FontWeight.W700 : FontWeight.W400;
        var style = s.Italic ? FontStyle.Italic : FontStyle.Normal;
        return new FontKey(s.FontFamily, weight, style);
    }

}
