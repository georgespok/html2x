using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box.Publishing;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Orchestrates block layout by mutating internal boxes while publishing immutable layout facts.
/// </summary>
/// <remarks>
/// The mutable box path remains an internal layout implementation detail. Production fragment projection
/// consumes <see cref="PublishedLayoutTree"/> so rendering does not read box internals.
/// </remarks>
internal sealed class BlockLayoutEngine
{
    private readonly InlineLayoutEngine _inlineEngine;
    private readonly BlockMeasurementService _measurement;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly ImageBlockLayoutApplier _imageBlockLayoutApplier;
    private readonly TableBlockLayoutApplier _tableBlockLayoutApplier;
    private readonly PublishedLayoutPublisher _publisher = new();
    private readonly BlockFlowLayoutExecutor _blockFlow;

    public BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        IDiagnosticsSink? diagnosticsSink = null)
        : this(
            inlineEngine,
            tableEngine,
            new BlockFormattingContext(),
            new ImageLayoutResolver(),
            diagnosticsSink)
    {
    }

    internal BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver imageResolver,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(inlineEngine);
        ArgumentNullException.ThrowIfNull(tableEngine);
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _measurement = new BlockMeasurementService(_blockFormattingContext);
        _inlineEngine = inlineEngine;
        _imageBlockLayoutApplier = new ImageBlockLayoutApplier(imageResolver);
        _tableBlockLayoutApplier = new TableBlockLayoutApplier(
            tableEngine,
            new TablePlacementApplier(),
            diagnosticsSink);
        _diagnosticsSink = diagnosticsSink;
        _blockFlow = new BlockFlowLayoutExecutor(
            _inlineEngine,
            _blockFormattingContext,
            _publisher,
            LayoutBlock,
            _diagnosticsSink);
    }

    public BoxTree Layout(BoxNode boxRoot, PageBox page)
    {
        if (boxRoot is null)
        {
            throw new ArgumentNullException(nameof(boxRoot));
        }

        ArgumentNullException.ThrowIfNull(page);

        // Mutable box output is retained for focused geometry tests.
        // Production fragment projection consumes PublishedLayoutTree.
        ResetPublishedLayoutState();

        var tree = new BoxTree();
        CopyPageTo(tree.Page, page);

        var contentArea = PageContentArea.From(page.Size, page.Margin);
        var contentX = contentArea.X;
        var contentY = contentArea.Y;
        var contentWidth = contentArea.Width;

        var candidates = SelectTopLevelCandidates(boxRoot);

        var layout = _blockFlow.LayoutStack(new BlockStackLayoutRequest(
            candidates,
            contentX,
            contentY,
            contentWidth));
        tree.Blocks.AddRange(layout.Blocks);

        return tree;
    }

    internal PublishedLayoutTree LayoutPublished(BoxNode boxRoot, PageBox page)
    {
        ArgumentNullException.ThrowIfNull(boxRoot);
        ArgumentNullException.ThrowIfNull(page);

        // Layout publishes immutable blocks directly while still applying internal box state.
        ResetPublishedLayoutState();

        var pageFacts = new PublishedPage(page.Size, page.Margin);
        var blocks = LayoutTopLevelBlocksAsPublished(boxRoot, page);

        return new PublishedLayoutTree(pageFacts, blocks);
    }

    private IReadOnlyList<PublishedBlock> LayoutTopLevelBlocksAsPublished(BoxNode boxRoot, PageBox page)
    {
        var contentArea = PageContentArea.From(page.Size, page.Margin);
        var candidates = SelectTopLevelCandidates(boxRoot);
        return _blockFlow.LayoutStack(new BlockStackLayoutRequest(
            candidates,
            contentArea.X,
            contentArea.Y,
            contentArea.Width)).PublishedBlocks;
    }

    private static IReadOnlyList<BoxNode> SelectTopLevelCandidates(BoxNode boxRoot)
    {
        if (boxRoot is TableBox tableRoot)
        {
            return [tableRoot];
        }

        if (boxRoot is BlockBox rootBlock)
        {
            return IsInlineOnlyBlock(rootBlock)
                ? [rootBlock]
                : rootBlock.Children;
        }

        return [boxRoot];
    }

    private static bool IsInlineOnlyBlock(BlockBox block)
    {
        var hasInline = block.Children.Any(c => c is InlineBox);
        var hasBlockOrTable = block.Children.Any(static c => c is BlockBox);
        return hasInline && !hasBlockOrTable;
    }

    private PublishedBlock LayoutBlock(BlockBox node, BlockLayoutRequest request)
    {
        return node switch
        {
            TableBox table => LayoutTableBlock(table, request),
            ImageBox image => LayoutImageBlock(image, request),
            RuleBox rule => LayoutRuleBlock(rule, request),
            _ => LayoutStandardBlock(node, request)
        };
    }

    internal PublishedBlock LayoutStandardBlock(BlockBox node, BlockLayoutRequest request)
    {
        var flowLayout = ApplyStandardBlockLayout(node, request);
        return _publisher.PublishBlock(
            node,
            flowLayout.PublishedInlineLayout,
            flowLayout.PublishedChildren,
            flowLayout.PublishedFlow);
    }

    private BlockFlowLayoutResult ApplyStandardBlockLayout(
        BlockBox node,
        BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var margin = measurement.Margin;
        var padding = measurement.Padding;
        var border = measurement.Border;

        var origin = BlockPlacementService.ResolveOrigin(request, margin);
        var x = origin.X;
        var y = origin.Y;

        var contentArea = ResolveInitialContentFlowArea(x, y, measurement, node.MarkerOffset);
        var contentWidthForChildren = contentArea.Width;
        var contentXForChildren = contentArea.X;
        var contentYForChildren = contentArea.Y;

        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        var flowLayout = _blockFlow.Layout(new BlockFlowLayoutRequest(
            node,
            contentXForChildren,
            contentYForChildren,
            contentWidthForChildren,
            contentYForChildren));
        var contentHeight = _measurement.ResolveContentHeight(
            node,
            flowLayout.ContentHeight);
        var borderBoxWidth = BoxGeometryFactory.RequireNonNegativeFinite(measurement.BorderBoxWidth);
        var contentBoxHeight = BoxGeometryFactory.RequireNonNegativeFinite(contentHeight);
        BlockLayoutState.Apply(
            node,
            measurement,
            CreateGeometryFromContentHeight(
                x,
                y,
                borderBoxWidth,
                contentBoxHeight,
                padding,
                border,
                node.MarkerOffset));

        return flowLayout;
    }

    internal PublishedBlock LayoutImageBlock(ImageBox node, BlockLayoutRequest request)
    {
        ApplyImageBlockLayout(node, request);
        return _publisher.PublishResolvedBlock(node);
    }

    private void ApplyImageBlockLayout(ImageBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        _imageBlockLayoutApplier.Apply(node, request, measurement);
    }

    internal PublishedBlock LayoutRuleBlock(RuleBox node, BlockLayoutRequest request)
    {
        ApplyRuleBlockLayout(node, request);
        return _publisher.PublishResolvedBlock(node);
    }

    private void ApplyRuleBlockLayout(RuleBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var origin = BlockPlacementService.ResolveOrigin(request, measurement.Margin);
        var x = origin.X;
        var y = origin.Y;

        BlockLayoutState.Apply(
            node,
            measurement,
            CreateGeometryFromContentHeight(
                x,
                y,
                measurement.BorderBoxWidth,
                0f,
                measurement.Padding,
                measurement.Border,
                node.MarkerOffset));
    }

    private float LayoutChildBlocks(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        var flowLayout = _blockFlow.Layout(new BlockFlowLayoutRequest(
            parent,
            contentX,
            cursorY,
            contentWidth,
            parentContentTop));
        _publisher.PublishBlock(
            parent,
            flowLayout.PublishedInlineLayout,
            flowLayout.PublishedChildren,
            flowLayout.PublishedFlow);

        return flowLayout.ContentHeight;
    }

    private static ContentFlowArea ResolveInitialContentFlowArea(
        float x,
        float y,
        BlockMeasurementBasis measurement,
        float markerOffset)
    {
        return BoxGeometryFactory.ResolveContentFlowArea(
            x,
            y,
            measurement.BorderBoxWidth,
            0f,
            measurement.Padding,
            measurement.Border,
            markerOffset);
    }

    private static UsedGeometry CreateGeometryFromContentHeight(
        float x,
        float y,
        float borderBoxWidth,
        float contentHeight,
        Spacing padding,
        Spacing border,
        float markerOffset)
    {
        return BoxGeometryFactory.FromBorderBoxWithContentHeight(
            x,
            y,
            borderBoxWidth,
            contentHeight,
            padding,
            border,
            markerOffset: markerOffset);
    }

    internal PublishedBlock LayoutTableBlock(TableBox node, BlockLayoutRequest request)
    {
        ApplyTableBlockLayout(node, request, LayoutChildBlocks);
        return _publisher.PublishResolvedBlock(node);
    }

    private void ApplyTableBlockLayout(
        TableBox node,
        BlockLayoutRequest request,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
    {
        _tableBlockLayoutApplier.Apply(node, request, layoutChildBlocks);
    }

    private static void CopyPageTo(PageBox target, PageBox source)
    {
        target.Size = source.Size;
        target.Margin = source.Margin;
    }

    private void ResetPublishedLayoutState()
    {
        _publisher.Reset();
    }

}
