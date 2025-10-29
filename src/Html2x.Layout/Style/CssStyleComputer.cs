using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;

namespace Html2x.Layout.Style;

/// <summary>
///     Minimal style computer: reads computed styles via AngleSharp and maps
///     only the properties we need for the MVP sample.
/// </summary>
public sealed class CssStyleComputer : IStyleComputer
{
    public StyleTree Compute(IDocument doc)
    {
        var tree = new StyleTree();

        var body = doc.Body as IElement;
        if (body is null)
        {
            return tree;
        }

        // Page margins ← body margin
        var bs = body.ComputeCurrentStyle();
        var mAll = GetPt(bs, "margin", 24);
        tree.Page.MarginTopPt = GetPt(bs, "margin-top", mAll);
        tree.Page.MarginRightPt = GetPt(bs, "margin-right", mAll);
        tree.Page.MarginBottomPt = GetPt(bs, "margin-bottom", mAll);
        tree.Page.MarginLeftPt = GetPt(bs, "margin-left", mAll);

        // Build element style subtree for <body> children we care about
        var root = new StyleNode { Element = body, Style = Read(bs, null) };
        foreach (var child in body.Children.Where(e =>
                     string.Equals(e.TagName, "H1", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(e.TagName, "P", StringComparison.OrdinalIgnoreCase)))
        {
            var cs = child.ComputeCurrentStyle();
            var node = new StyleNode { Element = child, Style = Read(cs, root.Style) };
            root.Children.Add(node);
        }

        tree.Root = root;
        return tree;
    }

    private static ComputedStyle Read(ICssStyleDeclaration s, ComputedStyle? inherit)
    {
        var st = new ComputedStyle
        {
            FontFamily = GetStr(s, "font-family", inherit?.FontFamily ?? "Arial"),
            FontSizePt = TryGetPt(s.GetPropertyValue("font-size"), out var fs) ? fs : inherit?.FontSizePt ?? 12,
            Bold = IsBold(GetStr(s, "font-weight")) || (inherit?.Bold ?? false),
            Italic = IsItalic(GetStr(s, "font-style")) || (inherit?.Italic ?? false),
            TextAlign = NormalizeAlign(GetStr(s, "text-align", inherit?.TextAlign ?? "left")),
            Color = GetStr(s, "color", inherit?.Color ?? "#000000"),
            MarginTopPt = GetPt(s, "margin-top", inherit?.MarginTopPt ?? 0),
            MarginRightPt = GetPt(s, "margin-right", inherit?.MarginRightPt ?? 0),
            MarginBottomPt = GetPt(s, "margin-bottom", inherit?.MarginBottomPt ?? 0),
            MarginLeftPt = GetPt(s, "margin-left", inherit?.MarginLeftPt ?? 0)
        };
        return st;
    }

    private static string GetStr(ICssStyleDeclaration s, string prop, string? fallback = null)
    {
        var v = s.GetPropertyValue(prop)?.Trim();
        return string.IsNullOrWhiteSpace(v) ? fallback ?? "" : v!;
    }

    private static string NormalizeAlign(string? v)
    {
        return string.IsNullOrWhiteSpace(v) ? "left" : v!.ToLowerInvariant();
    }

    private static bool IsBold(string? v)
    {
        if (string.IsNullOrWhiteSpace(v))
        {
            return false;
        }

        if (v!.Equals("bold", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(v, out var n) && n >= 600;
    }

    private static bool IsItalic(string? v)
    {
        return string.Equals(v, "italic", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(v, "oblique", StringComparison.OrdinalIgnoreCase);
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

        if (v.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(v[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out pt);
        }

        if (v.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
            float.TryParse(v[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var px))
        {
            pt = (float)(px * (72.0 / 96.0));
            return true;
        }

        if (v == "0")
        {
            pt = 0;
            return true;
        }

        return false;
    }
}