using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using System.Globalization;
using static Html2x.Layout.HtmlCssConstants;

namespace Html2x.Layout.Style;

/// <summary>
/// Computes simplified CSS styles for supported HTML tags
/// using AngleSharp's computed style API.
/// </summary>
public sealed class CssStyleComputer : IStyleComputer
{
    private static readonly HashSet<string> SupportedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        HtmlTags.Body, HtmlTags.H1, HtmlTags.H2, HtmlTags.H3, HtmlTags.H4, HtmlTags.H5, HtmlTags.H6,
        HtmlTags.P, HtmlTags.Span, HtmlTags.Div,
        HtmlTags.Table, HtmlTags.Tr, HtmlTags.Td, HtmlTags.Th,
        HtmlTags.Img, HtmlTags.Br,
        HtmlTags.Ul, HtmlTags.Ol, HtmlTags.Li,
        HtmlTags.Section, HtmlTags.Main, HtmlTags.Header, HtmlTags.Footer,
        HtmlTags.B, HtmlTags.I, HtmlTags.Strong
    };

    // Optional semantic defaults (MVP)
    // Apply font-size defaults only if no author font-size is specified for the element.
    private static readonly Dictionary<string, Action<ComputedStyle>> DefaultStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        [HtmlTags.H1] = s => s.FontSizePt = 18,
        [HtmlTags.H2] = s => s.FontSizePt = 16,
        [HtmlTags.H3] = s => s.FontSizePt = 14,
        [HtmlTags.Strong] = s => s.Bold = true,
        [HtmlTags.B] = s => s.Bold = true,
        [HtmlTags.I] = s => s.Italic = true,
        [HtmlTags.Th] = s => s.Bold = true
    };

    public StyleTree Compute(IDocument doc)
    {
        var tree = new StyleTree();

        var body = doc.Body as IElement;
        if (body is null)
        {
            return tree;
        }

        var bodyStyle = body.ComputeCurrentStyle();
        ApplyPageMargins(tree, bodyStyle);

        // recursively build the style tree starting from <body>
        tree.Root = BuildNodeRecursive(body, null);
        return tree;
    }

    private static StyleNode BuildNodeRecursive(IElement element, ComputedStyle? parentStyle)
    {
        var css = element.ComputeCurrentStyle();
        var computed = Read(element, css, parentStyle);

        var node = new StyleNode
        {
            Element = element,
            Style = computed
        };

        foreach (var child in element.Children)
        {
            if (!SupportedTags.Contains(child.TagName))
            {
                continue;
            }

            var childNode = BuildNodeRecursive(child, computed);
            node.Children.Add(childNode);
        }

        return node;
    }

    // Maps CSS → ComputedStyle
    private static ComputedStyle Read(IElement element, ICssStyleDeclaration s, ComputedStyle? inherit)
    {
        var style = new ComputedStyle
        {
            FontFamily = GetStr(s, HtmlCssConstants.CssProperties.FontFamily,
                inherit?.FontFamily ?? HtmlCssConstants.Defaults.FontFamily),

            FontSizePt = TryGetPt(s.GetPropertyValue(HtmlCssConstants.CssProperties.FontSize), out var fs)
                ? fs
                : inherit?.FontSizePt ?? HtmlCssConstants.Defaults.DefaultFontSizePt,

            Bold = IsBold(GetStr(s, HtmlCssConstants.CssProperties.FontWeight)) || (inherit?.Bold ?? false),
            Italic = IsItalic(GetStr(s, HtmlCssConstants.CssProperties.FontStyle)) || (inherit?.Italic ?? false),

            TextAlign = NormalizeAlign(GetStr(s, HtmlCssConstants.CssProperties.TextAlign,
                inherit?.TextAlign ?? HtmlCssConstants.Defaults.TextAlign)),

            Color = GetStr(s, HtmlCssConstants.CssProperties.Color, inherit?.Color ?? HtmlCssConstants.Defaults.Color),

            MarginTopPt = GetPt(s, HtmlCssConstants.CssProperties.MarginTop, 0),
            MarginRightPt = GetPt(s, HtmlCssConstants.CssProperties.MarginRight, 0),
            MarginBottomPt = GetPt(s, HtmlCssConstants.CssProperties.MarginBottom, 0),
            MarginLeftPt = GetPt(s, HtmlCssConstants.CssProperties.MarginLeft, 0)
        };

        // Apply semantic defaults (optional)
        if (!HasAuthorFontSize(element) && DefaultStyles.TryGetValue(element.TagName, out var apply))
        {
            apply(style);
        }

        return style;
    }

    private static bool HasAuthorFontSize(IElement element)
    {
        var inline = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Style);
        if (string.IsNullOrWhiteSpace(inline))
        {
            return false;
        }

        // Detect explicit font-size or font shorthand in inline styles.
        var s = inline!;
        return s.IndexOf(HtmlCssConstants.CssProperties.FontSize, StringComparison.OrdinalIgnoreCase) >= 0
               || s.Contains(HtmlCssConstants.CssShorthand.Font + ":", StringComparison.OrdinalIgnoreCase);
    }

    // Page margin mapping (<body> → page margins)
    private static void ApplyPageMargins(StyleTree tree, ICssStyleDeclaration s)
    {
        float? all = TryGetPt(s.GetPropertyValue(HtmlCssConstants.CssProperties.Margin), out var a) ? a : null;

        tree.Page.MarginTopPt = GetPt(s, HtmlCssConstants.CssProperties.MarginTop, all ?? 0);
        tree.Page.MarginRightPt = GetPt(s, HtmlCssConstants.CssProperties.MarginRight, all ?? 0);
        tree.Page.MarginBottomPt = GetPt(s, HtmlCssConstants.CssProperties.MarginBottom, all ?? 0);
        tree.Page.MarginLeftPt = GetPt(s, HtmlCssConstants.CssProperties.MarginLeft, all ?? 0);
    }

    private static string GetStr(ICssStyleDeclaration s, string prop, string? fallback = null)
    {
        var v = s.GetPropertyValue(prop)?.Trim();
        return string.IsNullOrWhiteSpace(v) ? fallback ?? "" : v!;
    }

    private static string NormalizeAlign(string? v)
    {
        return string.IsNullOrWhiteSpace(v)
            ? HtmlCssConstants.Defaults.TextAlign
            : v!.ToLowerInvariant();
    }

    private static bool IsBold(string? v)
    {
        if (string.IsNullOrWhiteSpace(v))
        {
            return false;
        }

        if (v!.Equals(HtmlCssConstants.CssValues.Bold, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(v, out var n) && n >= 600;
    }

    private static bool IsItalic(string? v)
    {
        return string.Equals(v, HtmlCssConstants.CssValues.Italic, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(v, HtmlCssConstants.CssValues.Oblique, StringComparison.OrdinalIgnoreCase);
    }

    private static float GetPt(ICssStyleDeclaration s, string prop, float fallback)
    {
        return TryGetPt(s.GetPropertyValue(prop), out var pt) ? pt : fallback;
    }

    private static bool TryGetPt(string? raw, out float pt)
    {
        pt = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var v = raw.Trim();

        if (v.EndsWith(HtmlCssConstants.CssUnits.Pt, StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(v[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out pt);
        }

        if (v.EndsWith(HtmlCssConstants.CssUnits.Px, StringComparison.OrdinalIgnoreCase) &&
            float.TryParse(v[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var px))
        {
            pt = (float)(px * (72.0 / 96.0)); // 1px = 0.75pt
            return true;
        }

        if (v != HtmlCssConstants.CssValues.Zero)
        {
            return false;
        }

        pt = 0;
        return true;

    }
}
