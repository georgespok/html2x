using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Produces resolved block sizing facts used by both layout and measurement paths.
/// </summary>
internal sealed class BlockSizingRules
{
    private readonly BlockFlowMeasurement _flowMeasurement;

    public BlockSizingRules()
        : this(new())
    {
    }

    internal BlockSizingRules(MarginCollapseRules marginCollapseRules)
    {
        _flowMeasurement = new(
            marginCollapseRules ?? throw new ArgumentNullException(nameof(marginCollapseRules)));
    }

    public BlockMeasurementBasis Prepare(BlockBox box, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(box);

        var style = box.Style;
        var margin = style.Margin.Safe();
        var padding = style.Padding.Safe();
        var border = Spacing.FromBorderEdges(style.Borders).Safe();

        var borderBoxWidth = BoxDimensionRules.ResolveBlockBorderBoxWidth(
            style,
            availableWidth,
            margin,
            padding,
            border);
        var contentFlowWidth =
            BoxDimensionRules.ResolveContentFlowWidth(borderBoxWidth, padding, border, box.MarkerOffset);

        return new(
            margin,
            padding,
            border,
            borderBoxWidth,
            contentFlowWidth);
    }

    public BlockMeasurementBasis PrepareAtomic(BlockBox box, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(box);

        var style = box.Style;
        var margin = style.Margin.Safe();
        var padding = style.Padding.Safe();
        var border = Spacing.FromBorderEdges(style.Borders).Safe();
        var borderBoxWidth = BoxDimensionRules.ResolveAtomicBorderBoxWidth(
            style,
            availableWidth,
            padding,
            border);
        var contentFlowWidth = BoxDimensionRules.ResolveContentFlowWidth(
            borderBoxWidth,
            padding,
            border,
            box.MarkerOffset);

        return new(
            margin,
            padding,
            border,
            borderBoxWidth,
            contentFlowWidth);
    }

    public float ResolveContentHeight(
        BlockBox box,
        float resolvedContentHeight,
        float minimumContentHeight = 0f)
    {
        ArgumentNullException.ThrowIfNull(box);
        return BoxDimensionRules.ResolveContentBoxHeight(
            box.Style,
            resolvedContentHeight,
            minimumContentHeight);
    }

    public float MeasureStackedChildBlocks(
        IEnumerable<BoxNode> children,
        float availableWidth,
        Func<BlockBox, float, float> measureBlockHeight,
        Func<TableBox, float, float> measureTableHeight,
        IDiagnosticsSink? diagnosticsSink = null,
        FormattingContextKind formattingContext = FormattingContextKind.Block)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(measureBlockHeight);
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        var result = _flowMeasurement.MeasureStackedChildren(
            children,
            availableWidth,
            measureBlockHeight,
            measureTableHeight,
            diagnosticsSink,
            formattingContext,
            nameof(BlockSizingRules));

        return result.HasBlocks ? result.TotalHeight : 0f;
    }
}