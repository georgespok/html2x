using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Style.Models;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Style.Computation;

/// <summary>
///     Maps CSS border declarations into the computed visual border model.
/// </summary>
internal sealed class BorderStyleMapper(CssValueParser parser)
{
    private readonly CssValueParser _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    private readonly CssLengthDeclarationReader _lengthReader = new(parser);

    public void ApplyBorders(
        ICssStyleDeclaration css,
        IElement element,
        ComputedStyleBuilder style,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ApplyBorderSide(
            css,
            element,
            HtmlCssVocabulary.CssSides.Top,
            w => style.Borders.TopWidth = w,
            s => style.Borders.TopStyle = s,
            c => style.Borders.TopColor = c,
            diagnosticsSink);

        ApplyBorderSide(
            css,
            element,
            HtmlCssVocabulary.CssSides.Right,
            w => style.Borders.RightWidth = w,
            s => style.Borders.RightStyle = s,
            c => style.Borders.RightColor = c,
            diagnosticsSink);

        ApplyBorderSide(
            css,
            element,
            HtmlCssVocabulary.CssSides.Bottom,
            w => style.Borders.BottomWidth = w,
            s => style.Borders.BottomStyle = s,
            c => style.Borders.BottomColor = c,
            diagnosticsSink);

        ApplyBorderSide(
            css,
            element,
            HtmlCssVocabulary.CssSides.Left,
            w => style.Borders.LeftWidth = w,
            s => style.Borders.LeftStyle = s,
            c => style.Borders.LeftColor = c,
            diagnosticsSink);
    }

    private void ApplyBorderSide(
        ICssStyleDeclaration css,
        IElement element,
        string side,
        Action<float> setWidth,
        Action<BorderLineStyle> setStyle,
        Action<ColorRgba?> setColor,
        IDiagnosticsSink? diagnosticsSink)
    {
        var widthProperty = $"border-{side}-width";
        var widthRaw = css.GetPropertyValue(widthProperty);
        var widthPt = _parser.ParseLengthPt(widthRaw);
        if (widthPt.HasValue)
        {
            setWidth(widthPt.Value);
        }

        EmitAuthoredInvalidBorderWidth(element, widthProperty, diagnosticsSink);

        var styleProperty = $"border-{side}-style";
        var styleRaw = css.GetPropertyValue(styleProperty);
        var lineStyle = ParseBorderStyle(styleRaw);
        setStyle(lineStyle);
        EmitAuthoredInvalidBorderStyle(element, styleProperty, lineStyle, diagnosticsSink);

        var colorProperty = $"border-{side}-color";
        var colorRaw = css.GetPropertyValue(colorProperty);
        setColor(ParseBorderColor(colorRaw));
        EmitAuthoredInvalidBorderColor(element, colorProperty, diagnosticsSink);
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
            HtmlCssVocabulary.CssValues.None => BorderLineStyle.None,
            HtmlCssVocabulary.CssValues.Solid => BorderLineStyle.Solid,
            HtmlCssVocabulary.CssValues.Dashed => BorderLineStyle.Dashed,
            HtmlCssVocabulary.CssValues.Dotted => BorderLineStyle.Dotted,
            _ => BorderLineStyle.None
        };
    }

    private static ColorRgba? ParseBorderColor(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return CssColorParser.Parse(raw.Trim(), ColorRgba.Black);
    }

    private void EmitAuthoredInvalidBorderWidth(
        IElement element,
        string property,
        IDiagnosticsSink? diagnosticsSink)
    {
        var authored = AuthoredCssDeclarationReader.GetValue(element, property);
        if (string.IsNullOrWhiteSpace(authored))
        {
            return;
        }

        _ = _lengthReader.TryParseLengthToken(
            authored,
            element,
            property,
            $"Unable to parse {property} as a supported border width.",
            diagnosticsSink,
            out _);
    }

    private static void EmitAuthoredInvalidBorderStyle(
        IElement element,
        string property,
        BorderLineStyle lineStyle,
        IDiagnosticsSink? diagnosticsSink)
    {
        var authored = AuthoredCssDeclarationReader.GetValue(element, property);
        if (string.IsNullOrWhiteSpace(authored) ||
            lineStyle != BorderLineStyle.None ||
            string.Equals(authored.Trim(), HtmlCssVocabulary.CssValues.None, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        StyleDiagnosticEmitter.EmitUnsupportedDeclaration(
            diagnosticsSink,
            element,
            property,
            authored.Trim(),
            $"Unsupported border style for {property}.");
    }

    private static void EmitAuthoredInvalidBorderColor(
        IElement element,
        string property,
        IDiagnosticsSink? diagnosticsSink)
    {
        var authored = AuthoredCssDeclarationReader.GetValue(element, property);
        if (string.IsNullOrWhiteSpace(authored) ||
            CssColorParser.TryParse(authored, out _))
        {
            return;
        }

        StyleDiagnosticEmitter.EmitIgnoredDeclaration(
            diagnosticsSink,
            element,
            property,
            authored.Trim(),
            null,
            $"Unable to parse {property} as a supported color.");
    }
}
