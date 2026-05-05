using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box.Publishing;
using Html2x.LayoutEngine.Geometry.Formatting;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Coordinates block layout, block-kind rule dispatch, and published layout output.
/// </summary>
/// <remarks>
/// The mutable box path remains an internal layout implementation detail. Production fragment projection
/// consumes <see cref="PublishedLayoutTree"/> so rendering does not read box internals.
/// </remarks>
internal sealed class BlockLayoutEngine
{
    private readonly PublishedLayoutWriter _publishedLayoutWriter = new();
    private readonly BlockFlowLayoutExecutor _blockFlow;
    private readonly BlockLayoutRuleSet _rules;
    private readonly StandardBlockLayoutRule _standardBlockRule;
    private readonly ImageBlockLayoutRule _imageBlockRule;
    private readonly RuleBlockLayoutRule _ruleBlockRule;
    private readonly TableBlockLayoutRule _tableBlockRule;

    public BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        IDiagnosticsSink? diagnosticsSink = null)
        : this(
            inlineEngine,
            new TableGridLayout(inlineEngine),
            new BlockFormattingContext(),
            new ImageLayoutResolver(),
            diagnosticsSink)
    {
        ArgumentNullException.ThrowIfNull(tableEngine);
    }

    internal BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver imageResolver,
        IDiagnosticsSink? diagnosticsSink = null)
        : this(
            inlineEngine,
            new TableGridLayout(inlineEngine, imageResolver),
            blockFormattingContext,
            imageResolver,
            diagnosticsSink)
    {
        ArgumentNullException.ThrowIfNull(tableEngine);
    }

    internal BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableGridLayout tableGridLayout,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver imageResolver,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(inlineEngine);
        ArgumentNullException.ThrowIfNull(tableGridLayout);
        var blockFormattingContext1 = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        var stateWriter = new LayoutBoxStateWriter();
        var sizingRules = new BoxSizingRules(blockFormattingContext1);

        BlockLayoutRuleSet? ruleSet = null;
        _blockFlow = new BlockFlowLayoutExecutor(
            inlineEngine,
            blockFormattingContext1,
            _publishedLayoutWriter,
            (block, request) => WriteRuleResult(
                ruleSet?.Layout(block, request)
                ?? throw new InvalidOperationException("Block layout rules were used before initialization.")),
            diagnosticsSink);

        _standardBlockRule = new StandardBlockLayoutRule(
            sizingRules,
            _blockFlow,
            stateWriter);
        _imageBlockRule = new ImageBlockLayoutRule(
            sizingRules,
            new ImageBlockLayoutApplier(imageResolver, stateWriter));
        _ruleBlockRule = new RuleBlockLayoutRule(
            sizingRules,
            stateWriter);
        _tableBlockRule = new TableBlockLayoutRule(
            new TableBlockLayoutApplier(
                tableGridLayout,
                new TablePlacementApplier(stateWriter),
                diagnosticsSink),
            LayoutChildBlocks);
        _rules = new BlockLayoutRuleSet(
        [
            _tableBlockRule,
            _imageBlockRule,
            _ruleBlockRule,
            _standardBlockRule
        ]);
        ruleSet = _rules;
    }

    internal PublishedLayoutTree LayoutPublished(BoxNode boxRoot, PageBox page)
    {
        ArgumentNullException.ThrowIfNull(boxRoot);
        ArgumentNullException.ThrowIfNull(page);

        _publishedLayoutWriter.Reset();

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

    internal PublishedBlock LayoutBlock(BlockBox node, BlockLayoutRequest request)
    {
        return WriteRuleResult(_rules.Layout(node, request));
    }

    internal PublishedBlock LayoutStandardBlock(BlockBox node, BlockLayoutRequest request)
    {
        return WriteRuleResult(_standardBlockRule.Layout(node, request));
    }

    internal PublishedBlock LayoutImageBlock(ImageBox node, BlockLayoutRequest request)
    {
        return WriteRuleResult(_imageBlockRule.Layout(node, request));
    }

    internal PublishedBlock LayoutRuleBlock(RuleBox node, BlockLayoutRequest request)
    {
        return WriteRuleResult(_ruleBlockRule.Layout(node, request));
    }

    internal PublishedBlock LayoutTableBlock(TableBox node, BlockLayoutRequest request)
    {
        return WriteRuleResult(_tableBlockRule.Layout(node, request));
    }

    private PublishedBlock WriteRuleResult(BlockLayoutRuleResult result)
    {
        if (result.InlineLayout is not null || result.Children.Count > 0 || result.Flow is not null)
        {
            return _publishedLayoutWriter.WriteBlock(
                result.Block,
                result.InlineLayout,
                result.Children,
                result.Flow);
        }

        return _publishedLayoutWriter.WriteResolvedBlock(result.Block);
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
        _publishedLayoutWriter.WriteBlock(
            parent,
            flowLayout.PublishedInlineLayout,
            flowLayout.PublishedChildren,
            flowLayout.PublishedFlow);

        return flowLayout.ContentHeight;
    }
}
