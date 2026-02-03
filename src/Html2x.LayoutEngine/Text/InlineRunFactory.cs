using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

internal sealed class InlineRunFactory(IFontMetricsProvider metrics)
{
    private readonly IFontMetricsProvider _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    public bool TryBuildInlineBlockRun(InlineBox inline, int runId, out TextRunInput run)
    {
        run = default!;
        return false;
    }

    public bool TryBuildLineBreakRunFromInlineStyle(InlineBox inline, int runId, out TextRunInput run)
    {
        return TryBuildLineBreakRun(inline, inline.Style, runId, out run);
    }

    public bool TryBuildInlineBlockLayout(
        InlineBox inline,
        float availableWidth,
        ITextMeasurer measurer,
        ILineHeightStrategy lineHeightStrategy,
        out InlineObjectLayout layout)
    {
        var builder = new InlineObjectLayoutBuilder(measurer, _metrics, lineHeightStrategy);
        return builder.TryBuildInlineBlockLayout(inline, availableWidth, out layout);
    }

    public bool TryBuildLineBreakRunFromBlockContext(InlineBox inline, ComputedStyle blockStyle, int runId, out TextRunInput run)
    {
        return TryBuildLineBreakRun(inline, blockStyle, runId, out run);
    }

    private bool TryBuildLineBreakRun(InlineBox inline, ComputedStyle style, int runId, out TextRunInput run)
    {
        if (!IsLineBreak(inline))
        {
            run = default!;
            return false;
        }

        var font = _metrics.GetFontKey(style);
        var fontSize = _metrics.GetFontSize(style);
        run = new TextRunInput(
            runId,
            inline,
            string.Empty,
            font,
            fontSize,
            style,
            PaddingLeft: 0f,
            PaddingRight: 0f,
            MarginLeft: 0f,
            MarginRight: 0f,
            Kind: TextRunKind.LineBreak);
        return true;
    }

    public bool TryBuildTextRun(InlineBox inline, int runId, out TextRunInput run)
    {
        if (string.IsNullOrEmpty(inline.TextContent))
        {
            run = default!;
            return false;
        }

        var font = _metrics.GetFontKey(inline.Style);
        var fontSize = _metrics.GetFontSize(inline.Style);
        var (paddingLeft, paddingRight, marginLeft, marginRight) = GetInlineSpacing(inline);
        run = new TextRunInput(
            runId,
            inline,
            inline.TextContent,
            font,
            fontSize,
            inline.Style,
            paddingLeft,
            paddingRight,
            marginLeft,
            marginRight);
        return true;
    }

    private static bool IsLineBreak(InlineBox inline)
        => string.Equals(inline.Element?.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase);

    private static (float PaddingLeft, float PaddingRight, float MarginLeft, float MarginRight) GetInlineSpacing(InlineBox inline)
    {
        var source = inline;
        if (source.Element is null && source.Parent is InlineBox parent && parent.Element is not null)
        {
            source = parent;
        }

        if (source.Element is null)
        {
            return (0f, 0f, 0f, 0f);
        }

        var padding = source.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(source.Style.Borders).Safe();
        var margin = source.Style.Margin.Safe();

        return (padding.Left + border.Left, padding.Right + border.Right, margin.Left, margin.Right);
    }

}
