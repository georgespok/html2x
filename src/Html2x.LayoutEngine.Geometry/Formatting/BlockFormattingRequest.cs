using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Formatting;

internal sealed record BlockFormattingRequest
{
    public BlockFormattingRequest(
        FormattingContextKind contextKind,
        BlockBox rootBlock,
        float availableWidth,
        bool isWidthUnbounded = false,
        object? pageContext = null,
        string consumerName = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool emitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null)
    {
        ContextKind = contextKind;
        RootBlock = rootBlock ?? throw new ArgumentNullException(nameof(rootBlock));
        AvailableWidth = availableWidth;
        IsWidthUnbounded = isWidthUnbounded;
        PageContext = pageContext;
        ConsumerName = string.IsNullOrWhiteSpace(consumerName) ? "unknown" : consumerName;
        DiagnosticsSink = diagnosticsSink;
        EmitDiagnostics = emitDiagnostics;
        BlockHeightMeasurer = blockHeightMeasurer;
        TableHeightMeasurer = tableHeightMeasurer;

        ValidateWidth(availableWidth, isWidthUnbounded);
    }

    public FormattingContextKind ContextKind { get; }

    public BlockBox RootBlock { get; }

    public float AvailableWidth { get; }

    public bool IsWidthUnbounded { get; }

    public object? PageContext { get; }

    public string ConsumerName { get; }

    public IDiagnosticsSink? DiagnosticsSink { get; }

    public bool EmitDiagnostics { get; }

    public Func<BlockBox, float, float>? BlockHeightMeasurer { get; }

    public Func<TableBox, float, float>? TableHeightMeasurer { get; }

    public static BlockFormattingRequest ForInlineBlock(
        BlockBox rootBlock,
        float availableWidth,
        string consumerName = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool emitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null)
    {
        return new BlockFormattingRequest(
            FormattingContextKind.InlineBlock,
            rootBlock,
            availableWidth,
            isWidthUnbounded: false,
            pageContext: null,
            consumerName,
            diagnosticsSink,
            emitDiagnostics,
            blockHeightMeasurer,
            tableHeightMeasurer);
    }

    public static BlockFormattingRequest ForTopLevel(
        BlockBox rootBlock,
        float availableWidth,
        object? pageContext = null,
        string consumerName = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool emitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null)
    {
        return new BlockFormattingRequest(
            FormattingContextKind.Block,
            rootBlock,
            availableWidth,
            isWidthUnbounded: false,
            pageContext,
            consumerName,
            diagnosticsSink,
            emitDiagnostics,
            blockHeightMeasurer,
            tableHeightMeasurer);
    }

    public static BlockFormattingRequest ForUnboundedWidth(
        FormattingContextKind contextKind,
        BlockBox rootBlock,
        object? pageContext = null,
        string consumerName = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool emitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null)
    {
        return new BlockFormattingRequest(
            contextKind,
            rootBlock,
            float.PositiveInfinity,
            isWidthUnbounded: true,
            pageContext,
            consumerName,
            diagnosticsSink,
            emitDiagnostics,
            blockHeightMeasurer,
            tableHeightMeasurer);
    }

    private static void ValidateWidth(float availableWidth, bool isWidthUnbounded)
    {
        if (isWidthUnbounded)
        {
            if (!float.IsPositiveInfinity(availableWidth))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableWidth),
                    "Unbounded width requests must use float.PositiveInfinity.");
            }

            return;
        }

        if (!float.IsFinite(availableWidth))
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableWidth),
                "Available width must be finite unless explicitly marked as unbounded.");
        }

        if (availableWidth < 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableWidth),
                "Available width cannot be negative.");
        }
    }
}
