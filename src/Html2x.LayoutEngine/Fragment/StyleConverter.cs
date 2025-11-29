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

    /// <summary>
    /// Resolve target image width/height using authored values and intrinsic aspect ratio.
    /// Falls back to intrinsic when both authored are missing; falls back to square if intrinsic unknown.
    /// </summary>
    public static (double Width, double Height) ResolveImageSize(
        double? authoredWidth,
        double? authoredHeight,
        double intrinsicWidth,
        double intrinsicHeight)
    {
        double? iw = intrinsicWidth > 0 ? intrinsicWidth : null;
        double? ih = intrinsicHeight > 0 ? intrinsicHeight : null;

        if (authoredWidth.HasValue && authoredHeight.HasValue)
            return (authoredWidth.Value, authoredHeight.Value);

        if (authoredWidth.HasValue && iw.HasValue && ih.HasValue)
            return (authoredWidth.Value, authoredWidth.Value * ih.Value / iw.Value);

        if (authoredHeight.HasValue && iw.HasValue && ih.HasValue)
            return (authoredHeight.Value * iw.Value / ih.Value, authoredHeight.Value);

        if (iw.HasValue && ih.HasValue)
            return (iw.Value, ih.Value);

        if (authoredWidth.HasValue)
            return (authoredWidth.Value, authoredWidth.Value); // square fallback

        if (authoredHeight.HasValue)
            return (authoredHeight.Value, authoredHeight.Value); // square fallback

        return (0, 0); // unknown size; caller should handle
    }
}
