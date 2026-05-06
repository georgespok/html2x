using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Measures inline-block content as an atomic inline object for current inline layout.
/// </summary>
internal sealed class AtomicInlineObjectLayout(
    ITextMeasurer measurer,
    IFontMetricsProvider metrics,
    ILineHeightStrategy lineHeightStrategy,
    BlockContentExtentMeasurement contentMeasurement,
    IImageSizingRules? imageResolver = null,
    IDiagnosticsSink? diagnosticsSink = null)
{
    private readonly BlockContentExtentMeasurement _blockContentMeasurement =
        contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement));

    private readonly IImageSizingRules _imageResolver = imageResolver ?? new ImageSizingRules();
    private readonly TextLineLayout _layoutEngine = new(measurer);

    private readonly ILineHeightStrategy _lineHeightStrategy =
        lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));

    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
    private readonly IFontMetricsProvider _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    private readonly InlineRunConstruction _runConstruction = new(
        metrics,
        contentMeasurement,
        imageResolver ?? new ImageSizingRules(),
        diagnosticsSink);

    private readonly BlockSizingRules _sizingRules = new(contentMeasurement.MarginCollapseRules);

    public InlineObjectLayout? MeasureInlineBlock(InlineBox inline, float availableWidth)
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

        var measurement = _sizingRules.PrepareAtomic(contentBox, availableWidth);

        if (contentBox is ImageBox imageBox)
        {
            return BuildImageInlineObject(imageBox, measurement);
        }

        return BuildContentInlineObject(contentBox, measurement);
    }

    private InlineObjectLayout BuildImageInlineObject(ImageBox imageBox, BlockMeasurementBasis measurement)
    {
        var image = _imageResolver.Resolve(imageBox, measurement.ContentFlowWidth);
        var resolvedLineHeight = ResolveLineHeight(imageBox);
        var resolvedBaseline = Math.Max(resolvedLineHeight, image.TotalHeight);

        return new(
            imageBox,
            new([], image.ContentHeight, image.ContentWidth),
            image.ContentWidth,
            image.ContentHeight,
            image.TotalWidth,
            image.TotalHeight,
            resolvedBaseline,
            image);
    }

    private InlineObjectLayout BuildContentInlineObject(BlockBox contentBox, BlockMeasurementBasis measurement)
    {
        var lineHeight = ResolveLineHeight(contentBox);
        var layoutResult = LayoutInlineContent(contentBox, measurement.ContentFlowWidth, lineHeight);
        var formattingResult = FormatBlockContent(contentBox, measurement.ContentFlowWidth);

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

    private TextLayoutResult LayoutInlineContent(BlockBox contentBox, float availableWidth, float lineHeight)
    {
        var runs = CollectInlineRuns(contentBox, availableWidth);
        return _layoutEngine.Layout(new(runs, availableWidth, lineHeight));
    }

    private BlockContentExtentMeasurementResult FormatBlockContent(BlockBox contentBox, float availableWidth)
    {
        if (float.IsFinite(availableWidth))
        {
            var request = BlockContentExtentMeasurementRequest.ForInlineBlock(
                contentBox,
                availableWidth,
                GeometryDiagnosticNames.Consumers.InlineLayoutEngine,
                diagnosticsSink,
                diagnosticsSink is not null);
            return _blockContentMeasurement.Measure(request);
        }

        var unboundedRequest = BlockContentExtentMeasurementRequest.ForUnboundedWidth(
            FormattingContextKind.InlineBlock,
            contentBox,
            GeometryDiagnosticNames.Consumers.InlineLayoutEngine,
            diagnosticsSink,
            diagnosticsSink is not null);
        return _blockContentMeasurement.Measure(unboundedRequest);
    }

    private static float ResolveMeasuredContentWidth(
        TextLayoutResult layoutResult,
        BlockContentExtentMeasurementResult formattingResult,
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
        BlockContentExtentMeasurementResult formattingResult)
    {
        if (!HasCanonicalBlockDescendants(contentBox, formattingResult))
        {
            return layoutResult.TotalHeight;
        }

        return Math.Max(layoutResult.TotalHeight, formattingResult.TotalHeight);
    }

    private static bool HasCanonicalBlockDescendants(
        BlockBox contentBox,
        BlockContentExtentMeasurementResult formattingResult)
    {
        return formattingResult.FormattedBlocks.Any(block => !ReferenceEquals(block, contentBox));
    }

    private float ResolveLineHeight(BlockBox contentBox)
    {
        var font = _metrics.GetFontKey(contentBox.Style);
        var fontSize = _metrics.GetFontSize(contentBox.Style);
        var metrics = _measurer.GetMetrics(font, fontSize);
        return _lineHeightStrategy.GetLineHeight(contentBox.Style, font, fontSize, metrics);
    }

    private IReadOnlyList<TextRunInput> CollectInlineRuns(BlockBox block, float availableWidth)
    {
        var collector = new InlineRunCollector(
            block.Style,
            availableWidth,
            _runConstruction,
            _measurer,
            _lineHeightStrategy);

        var walker = new InlineRunTreeWalker(collector);
        walker.CollectInlineObjectContent(block);

        return collector.Runs;
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

        baseline += ResolveLineAscent(layoutResult.Lines[^1]);
        return baseline;
    }

    private static float ResolveLineAscent(TextLayoutLine line)
    {
        var ascent = 0f;
        foreach (var run in line.Runs)
        {
            ascent = Math.Max(ascent, run.Ascent);
        }

        return ascent;
    }
}