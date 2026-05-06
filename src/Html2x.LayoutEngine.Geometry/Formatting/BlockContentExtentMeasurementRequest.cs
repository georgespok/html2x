using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Formatting;

internal sealed record BlockContentExtentMeasurementRequest
{
    public BlockContentExtentMeasurementRequest(
        FormattingContextKind contextKind,
        BlockBox rootBlock,
        float availableWidth,
        bool isWidthUnbounded = false,
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

    public string ConsumerName { get; }

    public IDiagnosticsSink? DiagnosticsSink { get; }

    public bool EmitDiagnostics { get; }

    public Func<BlockBox, float, float>? BlockHeightMeasurer { get; }

    public Func<TableBox, float, float>? TableHeightMeasurer { get; }

    public static BlockContentExtentMeasurementRequest ForInlineBlock(
        BlockBox rootBlock,
        float availableWidth,
        string consumerName = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool emitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null) =>
        new(
            FormattingContextKind.InlineBlock,
            rootBlock,
            availableWidth,
            false,
            consumerName,
            diagnosticsSink,
            emitDiagnostics,
            blockHeightMeasurer,
            tableHeightMeasurer);

    public static BlockContentExtentMeasurementRequest ForUnboundedWidth(
        FormattingContextKind contextKind,
        BlockBox rootBlock,
        string consumerName = "unknown",
        IDiagnosticsSink? diagnosticsSink = null,
        bool emitDiagnostics = false,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null) =>
        new(
            contextKind,
            rootBlock,
            float.PositiveInfinity,
            true,
            consumerName,
            diagnosticsSink,
            emitDiagnostics,
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