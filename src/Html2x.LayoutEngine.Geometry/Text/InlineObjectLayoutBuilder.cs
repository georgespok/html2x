using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

/// <summary>
/// Measures and prepares inline-block content as an atomic inline object for current inline layout.
/// </summary>
internal sealed class InlineObjectLayoutBuilder(
    ITextMeasurer measurer,
    IFontMetricsProvider metrics,
    ILineHeightStrategy lineHeightStrategy,
    IBlockFormattingContext blockFormattingContext,
    IImageLayoutResolver? imageResolver = null,
    DiagnosticsSession? diagnosticsSession = null)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
    private readonly IFontMetricsProvider _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILineHeightStrategy _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
    private readonly InlineRunFactory _runFactory = new(
        metrics,
        blockFormattingContext,
        imageResolver ?? new ImageLayoutResolver(),
        diagnosticsSession);
    private readonly TextLayoutEngine _layoutEngine = new(measurer);
    private readonly IBlockFormattingContext _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
    private readonly IBlockGeometryMeasurer _geometryMeasurer = new BlockMeasurementService(blockFormattingContext);
    private readonly IImageLayoutResolver _imageResolver = imageResolver ?? new ImageLayoutResolver();
    private readonly DiagnosticsSession? _diagnosticsSession = diagnosticsSession;

    public bool TryBuildInlineBlockLayout(InlineBox inline, float availableWidth, out InlineObjectLayout layout)
    {
        if (inline.Role != BoxRole.InlineBlock)
        {
            layout = default!;
            return false;
        }

        var contentBox = inline.Children.OfType<BlockBox>().FirstOrDefault();
        if (contentBox is null)
        {
            layout = default!;
            return false;
        }

        var measurement = _geometryMeasurer.PrepareAtomic(contentBox, availableWidth);
        var padding = measurement.Padding;
        var border = measurement.Border;
        var contentAvailableWidth = measurement.ContentFlowWidth;

        if (contentBox is ImageBox imageBox)
        {
            var image = _imageResolver.Resolve(imageBox, contentAvailableWidth);
            var resolvedLineHeight = ResolveLineHeight(contentBox);
            var resolvedBaseline = Math.Max(resolvedLineHeight, image.TotalHeight);

            layout = new InlineObjectLayout(
                contentBox,
                new TextLayoutResult([], image.ContentHeight, image.ContentWidth),
                image.ContentWidth,
                image.ContentHeight,
                image.TotalWidth,
                image.TotalHeight,
                resolvedBaseline,
                image);
            return true;
        }

        var lineHeight = ResolveLineHeight(contentBox);
        var runs = CollectInlineRuns(contentBox, contentAvailableWidth);
        var layoutResult = _layoutEngine.Layout(new TextLayoutInput(runs, contentAvailableWidth, lineHeight));
        var formattingResult = FormatBlockContent(contentBox, contentAvailableWidth);

        var measuredContentFlowWidth = ResolveMeasuredContentWidth(layoutResult, formattingResult, contentAvailableWidth);
        var measuredContentBoxWidth = measuredContentFlowWidth + contentBox.MarkerOffset;
        var measuredContentHeight = ResolveContentHeight(contentBox, layoutResult, formattingResult);
        var totalWidth = ResolveUsedBorderWidth(contentBox.Style, measuredContentBoxWidth, padding, border);
        var contentWidth = BoxDimensionResolver.ResolveContentFlowWidth(
            totalWidth,
            padding,
            border,
            contentBox.MarkerOffset);
        var contentHeight = _geometryMeasurer.ResolveContentHeight(contentBox, measuredContentHeight);
        var totalHeight = BoxGeometryFactory.ResolveBorderBoxHeight(contentHeight, padding, border);
        var baseline = ResolveBaseline(layoutResult, padding, border, totalHeight);

        layout = new InlineObjectLayout(
            contentBox,
            layoutResult,
            contentWidth,
            contentHeight,
            totalWidth,
            totalHeight,
            baseline);
        return true;
    }

    public bool TryLayoutInlineBlock(InlineBox inline, float availableWidth, out InlineObjectLayout layout)
    {
        return TryBuildInlineBlockLayout(inline, availableWidth, out layout);
    }

    private BlockFormattingResult FormatBlockContent(BlockBox contentBox, float availableWidth)
    {
        if (float.IsFinite(availableWidth))
        {
            var request = BlockFormattingRequest.ForInlineBlock(
                contentBox,
                availableWidth,
                consumerName: nameof(InlineLayoutEngine),
                diagnosticsSession: _diagnosticsSession,
                emitDiagnostics: _diagnosticsSession is not null);
            return _blockFormattingContext.Format(request);
        }

        var unboundedRequest = BlockFormattingRequest.ForUnboundedWidth(
            FormattingContextKind.InlineBlock,
            contentBox,
            consumerName: nameof(InlineLayoutEngine),
            diagnosticsSession: _diagnosticsSession,
            emitDiagnostics: _diagnosticsSession is not null);
        return _blockFormattingContext.Format(unboundedRequest);
    }

    private static float ResolveMeasuredContentWidth(
        TextLayoutResult layoutResult,
        BlockFormattingResult formattingResult,
        float contentAvailableWidth)
    {
        var maxLineWidth = Math.Max(layoutResult.MaxLineWidth, formattingResult.TotalWidth);
        return ResolveFinalContentWidth(contentAvailableWidth, maxLineWidth);
    }

    private static float ResolveUsedBorderWidth(
        ComputedStyle style,
        float measuredContentBoxWidth,
        Spacing padding,
        Spacing border)
    {
        return BoxDimensionResolver.ResolveIntrinsicBorderBoxWidth(style, measuredContentBoxWidth, padding, border);
    }

    private static float ResolveContentHeight(
        BlockBox contentBox,
        TextLayoutResult layoutResult,
        BlockFormattingResult formattingResult)
    {
        if (!HasCanonicalBlockDescendants(contentBox, formattingResult))
        {
            return layoutResult.TotalHeight;
        }

        return Math.Max(layoutResult.TotalHeight, formattingResult.TotalHeight);
    }

    private static bool HasCanonicalBlockDescendants(
        BlockBox contentBox,
        BlockFormattingResult formattingResult)
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
            _runFactory,
            _measurer,
            _lineHeightStrategy);

        CollectRunsFromNodes(block.Children, block.Style, collector);
        collector.TrimBoundaryLineBreaks();

        return collector.Runs;
    }

    private void CollectRunsFromNodes(
        IEnumerable<BoxNode> nodes,
        ComputedStyle blockStyle,
        InlineRunCollector collector)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case InlineBox inline:
                    CollectInlineRuns(inline, blockStyle, collector);
                    break;
                case BlockBox blockChild:
                {
                    CollectRunsFromBlockChild(blockChild, blockStyle, collector);
                    break;
                }
                default:
                    if (node.Children.Count > 0)
                    {
                        CollectRunsFromNodes(node.Children, blockStyle, collector);
                    }

                    break;
            }
        }
    }

    private void CollectRunsFromBlockChild(
        BlockBox blockChild,
        ComputedStyle parentStyle,
        InlineRunCollector collector)
    {
        var runCountBeforeBoundary = collector.Count;
        AppendBlockBoundaryBreak(parentStyle, collector);
        var runCountAfterBoundary = collector.Count;

        CollectRunsFromNodes(blockChild.Children, blockChild.Style, collector);

        if (collector.Count > runCountAfterBoundary)
        {
            AppendBlockBoundaryBreak(parentStyle, collector);
            return;
        }

        if (collector.Count > runCountBeforeBoundary && collector.LastKind == TextRunKind.LineBreak)
        {
            collector.RemoveLast();
        }
    }

    private void CollectInlineRuns(
        InlineBox inline,
        ComputedStyle blockStyle,
        InlineRunCollector collector)
    {
        if (collector.TryAppendInlineBlockRun(inline))
        {
            return;
        }

        if (collector.TryAppendLineBreakRun(inline, blockStyle))
        {
            return;
        }

        _ = collector.TryAppendTextRun(inline);

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(childInline, blockStyle, collector);
        }
    }

    private static void AppendBlockBoundaryBreak(ComputedStyle style, InlineRunCollector collector)
    {
        if (collector.Count == 0 || collector.LastKind == TextRunKind.LineBreak)
        {
            return;
        }

        collector.AppendSyntheticLineBreakRun(style);
    }

    private static float ResolveFinalContentWidth(float availableWidth, float measuredWidth)
    {
        if (!float.IsFinite(availableWidth))
        {
            return BoxGeometryFactory.RequireNonNegativeFinite(measuredWidth);
        }

        return BoxGeometryFactory.RequireNonNegativeFinite(Math.Min(availableWidth, measuredWidth));
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
