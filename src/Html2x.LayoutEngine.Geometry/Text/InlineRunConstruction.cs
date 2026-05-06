using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Styles;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

internal sealed class InlineRunConstruction
{
    private readonly BlockContentExtentMeasurement _blockContentMeasurement;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly IImageSizingRules? _imageResolver;
    private readonly IFontMetricsProvider _metrics;

    public InlineRunConstruction(IFontMetricsProvider metrics)
        : this(metrics, new())
    {
    }

    internal InlineRunConstruction(
        IFontMetricsProvider metrics,
        BlockContentExtentMeasurement contentMeasurement,
        IImageSizingRules? imageResolver = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _blockContentMeasurement = contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement));
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
            0f,
            0f,
            margin.Left,
            margin.Right,
            TextRunKind.InlineObject,
            inlineLayout);
    }

    public TextRunInput? BuildLineBreakRunFromInlineStyle(InlineBox inline, int runId) =>
        BuildLineBreakRun(inline, inline.Style, runId);

    public InlineObjectLayout? BuildInlineBlockLayout(
        InlineBox inline,
        float availableWidth,
        ITextMeasurer measurer,
        ILineHeightStrategy lineHeightStrategy)
    {
        var layout = new AtomicInlineObjectLayout(
            measurer,
            _metrics,
            lineHeightStrategy,
            _blockContentMeasurement,
            _imageResolver,
            _diagnosticsSink);
        return layout.MeasureInlineBlock(inline, availableWidth);
    }

    public TextRunInput? BuildLineBreakRunFromBlockContext(InlineBox inline, ComputedStyle blockStyle, int runId) =>
        BuildLineBreakRun(inline, blockStyle, runId);

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
            0f,
            0f,
            0f,
            0f,
            TextRunKind.LineBreak);
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
            0f,
            0f,
            0f,
            0f,
            TextRunKind.LineBreak);
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
        => HtmlElementRules.IsLineBreak(inline.Element);

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
        return new(
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

    private static (float PaddingLeft, float PaddingRight, float MarginLeft, float MarginRight) GetInlineSpacing(
        InlineBox inline)
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