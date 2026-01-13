using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
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
    public static SizePx ResolveImageSize(SizePx authored, SizePx intrinsic)
    {
        var iw = intrinsic.Width is > 0 ? intrinsic.Width : null;
        var ih = intrinsic.Height is > 0 ? intrinsic.Height : null;

        if (authored.HasWidth && authored.HasHeight)
        {
            return new SizePx(authored.Width, authored.Height);
        }

        if (authored.HasWidth && iw.HasValue && ih.HasValue)
        {
            var authoredWidth = authored.Width.GetValueOrDefault();
            return new SizePx(
                authoredWidth,
                authoredWidth * ih.Value / iw.Value);
        }

        if (authored.HasHeight && iw.HasValue && ih.HasValue)
        {
            var authoredHeight = authored.Height.GetValueOrDefault();
            return new SizePx(
                authoredHeight * iw.Value / ih.Value,
                authoredHeight);
        }

        if (iw.HasValue && ih.HasValue)
        {
            return new SizePx(iw.Value, ih.Value);
        }

        if (authored.HasWidth)
        {
            var authoredWidth = authored.Width.GetValueOrDefault();
            return new SizePx(authoredWidth, authoredWidth); // square fallback
        }

        if (authored.HasHeight)
        {
            var authoredHeight = authored.Height.GetValueOrDefault();
            return new SizePx(authoredHeight, authoredHeight); // square fallback
        }

        return new SizePx(0d, 0d); // unknown size; caller should handle
    }
}
