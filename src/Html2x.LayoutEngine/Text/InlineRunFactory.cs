using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

internal sealed class InlineRunFactory
{
    private readonly IFontMetricsProvider _metrics;
    private readonly IBlockFormattingContext _blockFormattingContext;

    public InlineRunFactory(IFontMetricsProvider metrics)
        : this(metrics, new BlockFormattingContext())
    {
    }

    internal InlineRunFactory(IFontMetricsProvider metrics, IBlockFormattingContext blockFormattingContext)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
    }

    public bool TryBuildInlineBlockRun(InlineBox inline, int runId, InlineObjectLayout? inlineLayout, out TextRunInput run)
    {
        run = default!;
        if (inline.Role != DisplayRole.InlineBlock || inlineLayout is null)
        {
            return false;
        }

        var margin = inline.Style.Margin.Safe();
        run = CreateRun(
            runId,
            inline,
            string.Empty,
            inline.Style,
            PaddingLeft: 0f,
            PaddingRight: 0f,
            MarginLeft: margin.Left,
            MarginRight: margin.Right,
            Kind: TextRunKind.InlineObject,
            InlineObject: inlineLayout);
        return true;
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
        var builder = new InlineObjectLayoutBuilder(measurer, _metrics, lineHeightStrategy, _blockFormattingContext);
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

        run = CreateRun(
            runId,
            inline,
            string.Empty,
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

        var (paddingLeft, paddingRight, marginLeft, marginRight) = GetInlineSpacing(inline);
        run = CreateRun(
            runId,
            inline,
            inline.TextContent,
            inline.Style,
            paddingLeft,
            paddingRight,
            marginLeft,
            marginRight);
        return true;
    }

    private static bool IsLineBreak(InlineBox inline)
        => string.Equals(inline.Element?.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase);

    private TextRunInput CreateRun(
        int runId,
        InlineBox source,
        string text,
        ComputedStyle style,
        float PaddingLeft,
        float PaddingRight,
        float MarginLeft,
        float MarginRight,
        TextRunKind Kind = TextRunKind.Normal,
        InlineObjectLayout? InlineObject = null)
    {
        var font = _metrics.GetFontKey(style);
        var fontSize = _metrics.GetFontSize(style);
        return new TextRunInput(
            runId,
            source,
            text,
            font,
            fontSize,
            style,
            PaddingLeft,
            PaddingRight,
            MarginLeft,
            MarginRight,
            Kind,
            InlineObject);
    }

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
