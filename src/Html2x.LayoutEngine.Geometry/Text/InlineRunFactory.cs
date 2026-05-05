using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Styles;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

internal sealed class InlineRunFactory
{
    private readonly IFontMetricsProvider _metrics;
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly IImageLayoutResolver? _imageResolver;
    private readonly IDiagnosticsSink? _diagnosticsSink;

    public InlineRunFactory(IFontMetricsProvider metrics)
        : this(metrics, new BlockFormattingContext(), imageResolver: null, diagnosticsSink: null)
    {
    }

    internal InlineRunFactory(
        IFontMetricsProvider metrics,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver? imageResolver = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _imageResolver = imageResolver;
        _diagnosticsSink = diagnosticsSink;
    }

    public TextRunInput? BuildInlineBlockRun(InlineBox inline, int runId, InlineObjectLayout? inlineLayout)
    {
        if (inline.Role != BoxRole.InlineBlock || inlineLayout is null)
        {
            return null;
        }

        var margin = inline.Style.Margin.Safe();
        return CreateRun(
            runId,
            inline,
            string.Empty,
            inline.Style,
            paddingLeft: 0f,
            paddingRight: 0f,
            marginLeft: margin.Left,
            marginRight: margin.Right,
            kind: TextRunKind.InlineObject,
            inlineObject: inlineLayout);
    }

    public TextRunInput? BuildLineBreakRunFromInlineStyle(InlineBox inline, int runId)
    {
        return BuildLineBreakRun(inline, inline.Style, runId);
    }

    public InlineObjectLayout? BuildInlineBlockLayout(
        InlineBox inline,
        float availableWidth,
        ITextMeasurer measurer,
        ILineHeightStrategy lineHeightStrategy)
    {
        var builder = new AtomicInlineObjectLayout(
            measurer,
            _metrics,
            lineHeightStrategy,
            _blockFormattingContext,
            _imageResolver,
            _diagnosticsSink);
        return builder.MeasureInlineBlock(inline, availableWidth);
    }

    public TextRunInput? BuildLineBreakRunFromBlockContext(InlineBox inline, ComputedStyle blockStyle, int runId)
    {
        return BuildLineBreakRun(inline, blockStyle, runId);
    }

    internal TextRunInput CreateSyntheticLineBreakRun(ComputedStyle style, int runId)
    {
        var source = new InlineBox(BoxRole.Inline)
        {
            Style = style
        };

        return CreateRun(
            runId,
            source,
            string.Empty,
            style,
            paddingLeft: 0f,
            paddingRight: 0f,
            marginLeft: 0f,
            marginRight: 0f,
            kind: TextRunKind.LineBreak);
    }

    private TextRunInput? BuildLineBreakRun(InlineBox inline, ComputedStyle style, int runId)
    {
        if (!IsLineBreak(inline))
        {
            return null;
        }

        return CreateRun(
            runId,
            inline,
            string.Empty,
            style,
            paddingLeft: 0f,
            paddingRight: 0f,
            marginLeft: 0f,
            marginRight: 0f,
            kind: TextRunKind.LineBreak);
    }

    public TextRunInput? BuildTextRun(InlineBox inline, int runId)
    {
        if (string.IsNullOrEmpty(inline.TextContent))
        {
            return null;
        }

        var (paddingLeft, paddingRight, marginLeft, marginRight) = GetInlineSpacing(inline);
        return CreateRun(
            runId,
            inline,
            inline.TextContent,
            inline.Style,
            paddingLeft,
            paddingRight,
            marginLeft,
            marginRight);
    }

    private static bool IsLineBreak(InlineBox inline)
        => HtmlElementClassifier.IsLineBreak(inline.Element);

    private TextRunInput CreateRun(
        int runId,
        InlineBox source,
        string text,
        ComputedStyle style,
        float paddingLeft,
        float paddingRight,
        float marginLeft,
        float marginRight,
        TextRunKind kind = TextRunKind.Normal,
        InlineObjectLayout? inlineObject = null)
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
            paddingLeft,
            paddingRight,
            marginLeft,
            marginRight,
            kind,
            inlineObject);
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
