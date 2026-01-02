using AngleSharp.Dom;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Encapsulates semantic defaults applied when author styles do not override them.
/// </summary>
public sealed class UserAgentDefaults : IUserAgentDefaults
{
    private static readonly IReadOnlyDictionary<string, Action<ComputedStyleBuilder>> FontSizeDefaults =
        new Dictionary<string, Action<ComputedStyleBuilder>>(StringComparer.OrdinalIgnoreCase)
        {
            [HtmlCssConstants.HtmlTags.H1] = s => s.FontSizePt = 18,
            [HtmlCssConstants.HtmlTags.H2] = s => s.FontSizePt = 16,
            [HtmlCssConstants.HtmlTags.H3] = s => s.FontSizePt = 14,
            [HtmlCssConstants.HtmlTags.H4] = s => s.FontSizePt = HtmlCssConstants.Defaults.DefaultFontSizePt,
            [HtmlCssConstants.HtmlTags.H5] = s => s.FontSizePt = 11,
            [HtmlCssConstants.HtmlTags.H6] = s => s.FontSizePt = 10
        };

    private const float HeadingGapPt = 5f;

    public void Apply(IElement element, ComputedStyleBuilder style, ComputedStyle? inheritedStyle)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (style is null)
        {
            throw new ArgumentNullException(nameof(style));
        }

        if (!AuthorOverridesFontSize(element) && FontSizeDefaults.TryGetValue(element.TagName, out var setFont))
        {
            setFont(style);
        }

        if (IsHeading(element) && !AuthorOverridesMargins(element))
        {
            style.Margin = new Spacing(HeadingGapPt, style.Margin.Right, HeadingGapPt, style.Margin.Left);
        }

        if (IsBoldTag(element))
        {
            style.Bold = true;
        }

        if (IsItalicTag(element))
        {
            style.Italic = true;
        }

        if (IsUnderlineTag(element))
        {
            style.Decorations |= TextDecorations.Underline;
        }

        if (IsStrikethroughTag(element))
        {
            style.Decorations |= TextDecorations.LineThrough;
        }

        if (IsTableHeader(element))
        {
            style.Bold = true;
        }
    }

    private static bool AuthorOverridesFontSize(IElement element)
    {
        var inline = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Style);
        if (string.IsNullOrWhiteSpace(inline))
        {
            return false;
        }

        var s = inline!;
        return s.IndexOf(HtmlCssConstants.CssProperties.FontSize, StringComparison.OrdinalIgnoreCase) >= 0
               || s.Contains(HtmlCssConstants.CssShorthand.Font + ":", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AuthorOverridesMargins(IElement element)
    {
        var inline = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Style);
        if (string.IsNullOrWhiteSpace(inline))
        {
            return false;
        }

        var s = inline!;
        return s.Contains(HtmlCssConstants.CssProperties.Margin + ":", StringComparison.OrdinalIgnoreCase)
               || s.Contains(HtmlCssConstants.CssProperties.MarginTop + ":", StringComparison.OrdinalIgnoreCase)
               || s.Contains(HtmlCssConstants.CssProperties.MarginBottom + ":", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHeading(IElement element)
    {
        var t = element.TagName;
        return string.Equals(t, HtmlCssConstants.HtmlTags.H1, StringComparison.OrdinalIgnoreCase)
               || string.Equals(t, HtmlCssConstants.HtmlTags.H2, StringComparison.OrdinalIgnoreCase)
               || string.Equals(t, HtmlCssConstants.HtmlTags.H3, StringComparison.OrdinalIgnoreCase)
               || string.Equals(t, HtmlCssConstants.HtmlTags.H4, StringComparison.OrdinalIgnoreCase)
               || string.Equals(t, HtmlCssConstants.HtmlTags.H5, StringComparison.OrdinalIgnoreCase)
               || string.Equals(t, HtmlCssConstants.HtmlTags.H6, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBoldTag(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Strong, StringComparison.OrdinalIgnoreCase)
               || string.Equals(element.TagName, HtmlCssConstants.HtmlTags.B, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsItalicTag(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.I, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnderlineTag(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.U, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStrikethroughTag(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.S, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTableHeader(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Th, StringComparison.OrdinalIgnoreCase);
    }
}
