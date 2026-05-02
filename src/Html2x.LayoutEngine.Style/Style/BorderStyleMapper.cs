using AngleSharp.Css.Dom;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Maps CSS border declarations into the computed visual border model.
/// </summary>
internal sealed class BorderStyleMapper(CssValueConverter converter)
{
    private readonly CssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));

    public void ApplyBorders(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        ApplyBorderSide(
            css,
            "top",
            w => style.Borders.TopWidth = w,
            s => style.Borders.TopStyle = s,
            c => style.Borders.TopColor = c);

        ApplyBorderSide(
            css,
            "right",
            w => style.Borders.RightWidth = w,
            s => style.Borders.RightStyle = s,
            c => style.Borders.RightColor = c);

        ApplyBorderSide(
            css,
            "bottom",
            w => style.Borders.BottomWidth = w,
            s => style.Borders.BottomStyle = s,
            c => style.Borders.BottomColor = c);

        ApplyBorderSide(
            css,
            "left",
            w => style.Borders.LeftWidth = w,
            s => style.Borders.LeftStyle = s,
            c => style.Borders.LeftColor = c);
    }

    private void ApplyBorderSide(
        ICssStyleDeclaration css,
        string side,
        Action<float> setWidth,
        Action<BorderLineStyle> setStyle,
        Action<ColorRgba?> setColor)
    {
        var widthRaw = css.GetPropertyValue($"border-{side}-width");
        if (_converter.TryGetLengthPt(widthRaw, out var widthPt))
        {
            setWidth(widthPt);
        }

        var styleRaw = css.GetPropertyValue($"border-{side}-style");
        setStyle(ParseBorderStyle(styleRaw));

        var colorRaw = css.GetPropertyValue($"border-{side}-color");
        setColor(ParseBorderColor(colorRaw));
    }

    private static BorderLineStyle ParseBorderStyle(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return BorderLineStyle.None;
        }

        var token = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?.ToLowerInvariant();

        return token switch
        {
            HtmlCssConstants.CssValues.None => BorderLineStyle.None,
            HtmlCssConstants.CssValues.Solid => BorderLineStyle.Solid,
            HtmlCssConstants.CssValues.Dashed => BorderLineStyle.Dashed,
            HtmlCssConstants.CssValues.Dotted => BorderLineStyle.Dotted,
            _ => BorderLineStyle.None
        };
    }

    private static ColorRgba? ParseBorderColor(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return ColorRgba.FromCss(raw.Trim(), ColorRgba.Black);
    }
}
