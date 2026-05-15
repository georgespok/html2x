using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Publishing;
using Html2x.LayoutEngine.Geometry.Tables;
using Html2x.LayoutEngine.Geometry.Writing;

namespace Html2x.LayoutEngine.Geometry.BlockFlow;

/// <summary>
///     Coordinates block layout, block-kind rule dispatch, and published layout output.
/// </summary>
/// <remarks>
///     The mutable box path remains an internal layout implementation detail. Production fragment tree building
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
        InlineFlowLayout inlineFlowLayout,
        TableGridLayout tableGridLayout,
        BlockFormattingMetricsMeasurement contentMeasurement,
        ImageSizingRules imageSizingRules,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(inlineFlowLayout);
        ArgumentNullException.ThrowIfNull(tableGridLayout);
        var resolvedContentMeasurement =
            contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement));
        var marginCollapseRules = resolvedContentMeasurement.MarginCollapseRules;
        var stateWriter = new LayoutBoxStateWriter();
        var tableStateWriter = new TableBoxStateWriter(stateWriter);
        var sizingRules = new BlockSizingRules(marginCollapseRules);

        _blockFlow = new(
            inlineFlowLayout,
            marginCollapseRules,
            stateWriter,
            LayoutChildBlock,
            diagnosticsSink);

        _standardBlockRule = new(
            sizingRules,
            _blockFlow,
            stateWriter);
        _imageBlockRule = new(
            sizingRules,
            new(imageSizingRules, stateWriter));
        _ruleBlockRule = new(
            sizingRules,
            stateWriter);
        _tableBlockRule = new(
            new(
                tableGridLayout,
                new(tableStateWriter),
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
        return _blockFlow.LayoutStack(request)
            .Results
            .Select(_publishedLayoutWriter.WriteRuleResult)
            .ToArray();
    }

    private BlockLayoutRuleResult LayoutChildBlock(BlockBox block, BlockLayoutRequest request) =>
        _rules.Layout(block, request);

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

    private float LayoutChildBlocks(BlockChildLayoutRequest request)
    {
        var flowLayout = _blockFlow.Layout(new(
            request.Parent,
            request.ContentX,
            request.CursorY,
            request.ContentWidth,
            request.ParentContentTop));
        _publishedLayoutWriter.WriteBlock(
            request.Parent,
            flowLayout.InlineLayout,
            flowLayout.Children,
            flowLayout.Flow);

        return flowLayout.ContentHeight;
    }
}
