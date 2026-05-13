using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Measurement;

internal sealed record BlockFormattingMetricsRequest
{
    public BlockFormattingMetricsRequest(
        FormattingContextKind formattingContext,
        BlockBox rootBlock,
        float availableWidth,
        bool isWidthUnbounded = false,
        string diagnosticConsumer = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool shouldEmitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null)
    {
        FormattingContext = formattingContext;
        RootBlock = rootBlock ?? throw new ArgumentNullException(nameof(rootBlock));
        AvailableWidth = availableWidth;
        IsWidthUnbounded = isWidthUnbounded;
        DiagnosticConsumer = string.IsNullOrWhiteSpace(diagnosticConsumer) ? "unknown" : diagnosticConsumer;
        DiagnosticsSink = diagnosticsSink;
        ShouldEmitDiagnostics = shouldEmitDiagnostics;
        BlockHeightMeasurer = blockHeightMeasurer;
        TableHeightMeasurer = tableHeightMeasurer;

        ValidateWidth(availableWidth, isWidthUnbounded);
    }

    public FormattingContextKind FormattingContext { get; }

    public BlockBox RootBlock { get; }

    public float AvailableWidth { get; }

    public bool IsWidthUnbounded { get; }

    public string DiagnosticConsumer { get; }

    public IDiagnosticsSink? DiagnosticsSink { get; }

    public bool ShouldEmitDiagnostics { get; }

    public Func<BlockBox, float, float>? BlockHeightMeasurer { get; }

    public Func<TableBox, float, float>? TableHeightMeasurer { get; }

    public static BlockFormattingMetricsRequest ForInlineBlock(
        BlockBox rootBlock,
        float availableWidth,
        string diagnosticConsumer = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool shouldEmitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null) =>
        new(
            FormattingContextKind.InlineBlock,
            rootBlock,
            availableWidth,
            false,
            diagnosticConsumer,
            diagnosticsSink,
            shouldEmitDiagnostics,
            blockHeightMeasurer,
            tableHeightMeasurer);

    public static BlockFormattingMetricsRequest ForUnboundedWidth(
        FormattingContextKind formattingContext,
        BlockBox rootBlock,
        string diagnosticConsumer = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool shouldEmitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null) =>
        new(
            formattingContext,
            rootBlock,
            float.PositiveInfinity,
            true,
            diagnosticConsumer,
            diagnosticsSink,
            shouldEmitDiagnostics,
            blockHeightMeasurer,
            tableHeightMeasurer);

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
