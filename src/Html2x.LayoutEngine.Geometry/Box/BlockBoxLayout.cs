using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box.Publishing;
using Html2x.LayoutEngine.Geometry.Formatting;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Coordinates block layout, block-kind rule dispatch, and published layout output.
/// </summary>
/// <remarks>
///     The mutable box path remains an internal layout implementation detail. Production fragment projection
///     consumes <see cref="PublishedLayoutTree" /> so rendering does not read box internals.
/// </remarks>
internal sealed class BlockBoxLayout
{
    private readonly BlockFlowLayout _blockFlow;
    private readonly ImageBlockLayoutRule _imageBlockRule;
    private readonly PublishedLayoutWriter _publishedLayoutWriter = new();
    private readonly RuleBlockLayoutRule _ruleBlockRule;
    private readonly BlockLayoutRuleSet _rules;
    private readonly StandardBlockLayoutRule _standardBlockRule;
    private readonly TableBlockLayoutRule _tableBlockRule;

    internal BlockBoxLayout(
        InlineFlowLayout inlineEngine,
        TableGridLayout tableGridLayout,
        BlockContentExtentMeasurement contentMeasurement,
        IImageSizingRules imageResolver,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(inlineEngine);
        ArgumentNullException.ThrowIfNull(tableGridLayout);
        var resolvedContentMeasurement =
            contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement));
        var marginCollapseRules = resolvedContentMeasurement.MarginCollapseRules;
        var stateWriter = new LayoutBoxStateWriter();
        var sizingRules = new BlockSizingRules(marginCollapseRules);

        _blockFlow = new(
            inlineEngine,
            marginCollapseRules,
            _publishedLayoutWriter,
            LayoutChildBlock,
            diagnosticsSink);

        _standardBlockRule = new(
            sizingRules,
            _blockFlow,
            stateWriter);
        _imageBlockRule = new(
            sizingRules,
            new(imageResolver, stateWriter));
        _ruleBlockRule = new(
            sizingRules,
            stateWriter);
        _tableBlockRule = new(
            new(
                tableGridLayout,
                new(stateWriter),
                diagnosticsSink),
            LayoutChildBlocks);
        _rules = CreateDefaultRuleSet(
            _tableBlockRule,
            _imageBlockRule,
            _ruleBlockRule,
            _standardBlockRule);
    }

    internal IReadOnlyList<PublishedBlock> LayoutBlockStack(BlockStackLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _publishedLayoutWriter.Reset();
        return _blockFlow.LayoutStack(request).PublishedBlocks;
    }

    internal PublishedBlock LayoutBlock(BlockBox node, BlockLayoutRequest request) =>
        WriteRuleResult(_rules.Layout(node, request));

    private PublishedBlock LayoutChildBlock(BlockBox block, BlockLayoutRequest request) =>
        WriteRuleResult(_rules.Layout(block, request));

    internal PublishedBlock LayoutStandardBlock(BlockBox node, BlockLayoutRequest request) =>
        WriteRuleResult(_standardBlockRule.Layout(node, request));

    internal PublishedBlock LayoutImageBlock(ImageBox node, BlockLayoutRequest request) =>
        WriteRuleResult(_imageBlockRule.Layout(node, request));

    internal PublishedBlock LayoutRuleBlock(RuleBox node, BlockLayoutRequest request) =>
        WriteRuleResult(_ruleBlockRule.Layout(node, request));

    internal PublishedBlock LayoutTableBlock(TableBox node, BlockLayoutRequest request) =>
        WriteRuleResult(_tableBlockRule.Layout(node, request));

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

    private static BlockLayoutRuleSet CreateDefaultRuleSet(
        TableBlockLayoutRule tableBlockRule,
        ImageBlockLayoutRule imageBlockRule,
        RuleBlockLayoutRule ruleBlockRule,
        StandardBlockLayoutRule standardBlockRule) =>
        new(
        [
            tableBlockRule,
            imageBlockRule,
            ruleBlockRule,
            standardBlockRule
        ]);

    private float LayoutChildBlocks(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        var flowLayout = _blockFlow.Layout(new(
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