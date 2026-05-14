using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Measures inline-block content as an atomic inline box for current inline layout.
/// </summary>
internal sealed class AtomicInlineBoxLayout
{
    private readonly BlockFormattingMetricsMeasurement _blockContentMeasurement;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly ImageSizingRules _imageSizingRules;
    private readonly InlineTextLayoutMeasurement _inlineTextMeasurement;
    private readonly BlockSizingRules _sizingRules;

    public AtomicInlineBoxLayout(
        ITextMeasurer measurer,
        IFontMetricsMeasurer metrics,
        ILineHeightStrategy lineHeightStrategy,
        BlockFormattingMetricsMeasurement contentMeasurement,
        ImageSizingRules? imageSizingRules = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(measurer);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(lineHeightStrategy);
        ArgumentNullException.ThrowIfNull(contentMeasurement);

        _blockContentMeasurement = contentMeasurement;
        _diagnosticsSink = diagnosticsSink;
        _imageSizingRules = imageSizingRules ?? new ImageSizingRules();
        _sizingRules = new(contentMeasurement.MarginCollapseRules);

        var runConstruction = new InlineRunConstruction(
            metrics,
            contentMeasurement,
            _imageSizingRules,
            diagnosticsSink);
        _inlineTextMeasurement = new(
            runConstruction,
            measurer,
            metrics,
            lineHeightStrategy);
    }

    public InlineBoxLayout? MeasureInlineBlock(InlineBox inline, float availableWidth)
    {
        if (inline.Role != BoxRole.InlineBlock)
        {
            return null;
        }

        var contentBox = inline.Children.OfType<BlockBox>().FirstOrDefault();
        if (contentBox is null)
        {
            return null;
        }

        var measurement = _sizingRules.ResolveAtomicMeasurementBasis(contentBox, availableWidth);

        if (contentBox is ImageBox imageBox)
        {
            return BuildImageInlineBox(imageBox, measurement);
        }

        return BuildContentInlineBox(contentBox, measurement);
    }

    private InlineBoxLayout BuildImageInlineBox(ImageBox imageBox, BlockMeasurementBasis measurement)
    {
        var image = _imageSizingRules.ResolveImageLayout(imageBox, measurement.ContentFlowWidth);
        var resolvedLineHeight = _inlineTextMeasurement.ResolveLineHeight(imageBox);
        var resolvedBaseline = Math.Max(resolvedLineHeight, image.BorderBoxHeight);

        return new(
            imageBox,
            new([], image.ContentHeight, image.ContentWidth),
            image.ContentWidth,
            image.ContentHeight,
            image.BorderBoxWidth,
            image.BorderBoxHeight,
            resolvedBaseline,
            image);
    }

    private InlineBoxLayout BuildContentInlineBox(BlockBox contentBox, BlockMeasurementBasis measurement)
    {
        var layoutResult = LayoutInlineContent(contentBox, measurement.ContentFlowWidth);
        var formattingResult = MeasureBlockFormattingMetrics(contentBox, measurement.ContentFlowWidth);

        var measuredContentFlowWidth =
            ResolveMeasuredContentWidth(layoutResult, formattingResult, measurement.ContentFlowWidth);
        var measuredContentBoxWidth = measuredContentFlowWidth + contentBox.MarkerOffset;
        var measuredContentHeight = ResolveContentHeight(contentBox, layoutResult, formattingResult);
        var totalWidth = ResolveUsedBorderWidth(contentBox.Style, measuredContentBoxWidth, measurement.Padding,
            measurement.Border);
        var contentWidth = BoxDimensionRules.ResolveContentFlowWidth(
            totalWidth,
            measurement.Padding,
            measurement.Border,
            contentBox.MarkerOffset);
        var contentHeight = _sizingRules.ResolveContentHeight(contentBox, measuredContentHeight);
        var totalHeight =
            UsedGeometryRules.ResolveBorderBoxHeight(contentHeight, measurement.Padding, measurement.Border);
        var baseline = ResolveBaseline(layoutResult, measurement.Padding, measurement.Border, totalHeight);

        return new(
            contentBox,
            layoutResult,
            contentWidth,
            contentHeight,
            totalWidth,
            totalHeight,
            baseline);
    }

    private TextLayoutResult LayoutInlineContent(BlockBox contentBox, float availableWidth)
    {
        return _inlineTextMeasurement.MeasureInlineBoxContent(contentBox, availableWidth);
    }

    private BlockFormattingMetricsResult MeasureBlockFormattingMetrics(BlockBox contentBox, float availableWidth)
    {
        if (float.IsFinite(availableWidth))
        {
            var request = BlockFormattingMetricsRequest.ForInlineBlock(
                contentBox,
                availableWidth,
                GeometryDiagnosticNames.Consumers.InlineFlowLayout,
                _diagnosticsSink,
                _diagnosticsSink is not null);
            return _blockContentMeasurement.Measure(request);
        }

        var unboundedRequest = BlockFormattingMetricsRequest.ForUnboundedWidth(
            FormattingContextKind.InlineBlock,
            contentBox,
            GeometryDiagnosticNames.Consumers.InlineFlowLayout,
            _diagnosticsSink,
            _diagnosticsSink is not null);
        return _blockContentMeasurement.Measure(unboundedRequest);
    }

    private static float ResolveMeasuredContentWidth(
        TextLayoutResult layoutResult,
        BlockFormattingMetricsResult formattingResult,
        float contentAvailableWidth)
    {
        var maxLineWidth = Math.Max(layoutResult.MaxLineWidth, formattingResult.TotalWidth);
        return ResolveFinalContentWidth(contentAvailableWidth, maxLineWidth);
    }

    private static float ResolveUsedBorderWidth(
        ComputedStyle style,
        float measuredContentBoxWidth,
        Spacing padding,
        Spacing border) =>
        BoxDimensionRules.ResolveIntrinsicBorderBoxWidth(style, measuredContentBoxWidth, padding, border);

    private static float ResolveContentHeight(
        BlockBox contentBox,
        TextLayoutResult layoutResult,
        BlockFormattingMetricsResult formattingResult)
    {
        if (!HasCanonicalBlockDescendants(contentBox, formattingResult))
        {
            return layoutResult.TotalHeight;
        }

        return Math.Max(layoutResult.TotalHeight, formattingResult.TotalHeight);
    }

    private static bool HasCanonicalBlockDescendants(
        BlockBox contentBox,
        BlockFormattingMetricsResult formattingResult)
    {
        return formattingResult.MeasuredBlocks.Any(block => !ReferenceEquals(block, contentBox));
    }

    private static float ResolveFinalContentWidth(float availableWidth, float measuredWidth)
    {
        if (!float.IsFinite(availableWidth))
        {
            return UsedGeometryRules.RequireNonNegativeFinite(measuredWidth);
        }

        return UsedGeometryRules.RequireNonNegativeFinite(Math.Min(availableWidth, measuredWidth));
    }

    private static float ResolveBaseline(
        TextLayoutResult layoutResult,
        Spacing padding,
        Spacing border,
        float totalHeight)
    {
        if (layoutResult.Lines.Count == 0)
        {
            return totalHeight;
        }

        var baseline = padding.Top + border.Top;
        for (var i = 0; i < layoutResult.Lines.Count - 1; i++)
        {
            baseline += layoutResult.Lines[i].LineHeight;
        }

        baseline += InlineBaselineRules.ResolveLineAscent(layoutResult.Lines[^1]);
        return baseline;
    }
}
