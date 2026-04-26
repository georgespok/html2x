using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Orchestrates current block layout by routing nodes to block, table, inline, and diagnostic formatting context owners.
/// </summary>
public sealed class BlockLayoutEngine : IBlockFormattingContextRunner
{
    private readonly IInlineFormattingContextRunner _inlineContext;
    private readonly BlockMeasurementService _measurement;
    private readonly ITableFormattingContextRunner _tableContext;
    private readonly DiagnosticsSession? _diagnosticsSession;
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly IImageLayoutResolver _imageResolver;
    private readonly FormattingContextLayoutDispatcher _formattingContexts;
    private readonly TablePlacementApplier _tablePlacementApplier;

    public BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        DiagnosticsSession? diagnosticsSession = null)
        : this(
            inlineEngine,
            tableEngine,
            new BlockFormattingContext(),
            new ImageLayoutResolver(),
            BlockLayoutStrategyRegistry.CreateDefault(),
            diagnosticsSession)
    {
    }

    internal BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver imageResolver,
        BlockLayoutStrategyRegistry layoutStrategies,
        DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(inlineEngine);
        ArgumentNullException.ThrowIfNull(tableEngine);
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _measurement = new BlockMeasurementService(_blockFormattingContext);
        _inlineContext = inlineEngine as IInlineFormattingContextRunner
            ?? new InlineFormattingContextRunnerAdapter(inlineEngine);
        _tableContext = tableEngine as ITableFormattingContextRunner
            ?? new TableFormattingContextRunnerAdapter(tableEngine);
        _imageResolver = imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));
        _formattingContexts = new FormattingContextLayoutDispatcher(layoutStrategies);
        _tablePlacementApplier = new TablePlacementApplier(_inlineContext);
        _diagnosticsSession = diagnosticsSession;
    }

    public BoxTree Layout(DisplayNode displayRoot, PageBox page)
    {
        if (displayRoot is null)
        {
            throw new ArgumentNullException(nameof(displayRoot));
        }

        var tree = new BoxTree();
        CopyPageTo(tree.Page, page);

        var contentX = page.Margin.Left;
        var contentY = page.Margin.Top;
        var pageSize = page.Size;
        var contentWidth = pageSize.Width - page.Margin.Left - page.Margin.Right;

        var candidates = SelectTopLevelCandidates(displayRoot);

        var currentY = contentY;
        var previousBottomMargin = 0f;

        foreach (var child in candidates)
        {
            if (child is not BlockBox block)
            {
                continue;
            }

            var marginTop = block.Style.Margin.Safe().Top;
            var collapsedTop = _blockFormattingContext.CollapseMargins(
                previousBottomMargin,
                marginTop,
                FormattingContextKind.Block,
                nameof(BlockLayoutEngine),
                _diagnosticsSession);
            var laidOutBlock = LayoutBlock(
                block,
                new BlockLayoutRequest(
                    contentX,
                    currentY,
                    contentWidth,
                    contentY,
                    previousBottomMargin,
                    collapsedTop));
            tree.Blocks.Add(laidOutBlock);
            currentY = laidOutBlock.Y + laidOutBlock.Height;
            previousBottomMargin = laidOutBlock.Margin.Bottom;
        }

        return tree;
    }

    private static IReadOnlyList<DisplayNode> SelectTopLevelCandidates(DisplayNode displayRoot)
    {
        if (displayRoot is TableBox tableRoot)
        {
            return [tableRoot];
        }

        if (displayRoot is BlockBox rootBlock)
        {
            return IsInlineOnlyBlock(rootBlock) 
                ? [rootBlock] 
                : rootBlock.Children;
        }

        return [displayRoot];
    }

    private static bool IsInlineOnlyBlock(BlockBox block)
    {
        var hasInline = block.Children.Any(c => c is InlineBox);
        var hasBlockOrTable = block.Children.Any(static c => c is BlockBox);
        return hasInline && !hasBlockOrTable;
    }

    private BlockBox LayoutBlock(BlockBox node, BlockLayoutRequest request)
    {
        return _formattingContexts.Layout(this, node, request);
    }

    internal BlockBox LayoutStandardBlock(BlockBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var margin = measurement.Margin;
        var padding = measurement.Padding;
        var border = measurement.Border;

        var origin = BlockPlacementService.ResolveOrigin(request, margin);
        var x = origin.X;
        var y = origin.Y;

        var contentArea = BoxGeometryFactory.ResolveContentArea(
            x,
            y,
            measurement.BorderBoxWidth,
            0f,
            padding,
            border,
            node.MarkerOffset);
        var contentWidthForChildren = contentArea.Width;
        var contentXForChildren = contentArea.X;
        var contentYForChildren = contentArea.Y;

        var inlineContentX = contentXForChildren;

        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        var childBlockFlowHeight = LayoutChildBlocks(
            node,
            contentXForChildren,
            contentYForChildren,
            contentWidthForChildren,
            contentYForChildren);
        var inlineLayout = _inlineContext.LayoutInlineContent(
            node,
            new InlineLayoutRequest(
                inlineContentX,
                contentYForChildren,
                contentWidthForChildren));
        var inlineFlowHeight = inlineLayout.TotalHeight;
        var resolvedContentHeight = ResolveFlowContentHeight(childBlockFlowHeight, inlineFlowHeight);
        var contentHeight = _measurement.ResolveContentHeight(
            node,
            resolvedContentHeight);
        var borderBoxWidth = BoxGeometryFactory.NormalizeNonNegative(measurement.BorderBoxWidth);
        var contentBoxHeight = BoxGeometryFactory.NormalizeNonNegative(contentHeight);
        var usedGeometry = BoxGeometryFactory.FromBorderBox(
            x,
            y,
            borderBoxWidth,
            contentBoxHeight + padding.Vertical + border.Vertical,
            padding,
            border,
            markerOffset: node.MarkerOffset);

        node.Margin = margin;
        node.Padding = padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.ApplyLayoutGeometry(usedGeometry);

        return node;
    }

    BlockBox IBlockFormattingContextRunner.LayoutBlock(BlockBox block, BlockLayoutRequest request)
    {
        return LayoutStandardBlock(block, request);
    }

    internal BlockBox LayoutImageBlock(ImageBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var image = _imageResolver.Resolve(node, measurement.ContentBoxWidth);
        var padding = measurement.Padding;
        var border = measurement.Border;
        var origin = BlockPlacementService.ResolveOrigin(request, measurement.Margin);
        var x = origin.X;
        var y = origin.Y;

        node.Src = image.Src;
        node.AuthoredSizePx = image.AuthoredSizePx;
        node.IntrinsicSizePx = image.IntrinsicSizePx;
        node.IsMissing = image.IsMissing;
        node.IsOversize = image.IsOversize;
        node.Margin = measurement.Margin;
        node.Padding = padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.ApplyLayoutGeometry(BoxGeometryFactory.FromBorderBox(
            x,
            y,
            image.TotalWidth,
            image.TotalHeight,
            padding,
            border,
            markerOffset: node.MarkerOffset));

        return node;
    }

    internal BlockBox LayoutRuleBlock(RuleBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var origin = BlockPlacementService.ResolveOrigin(request, measurement.Margin);
        var x = origin.X;
        var y = origin.Y;

        node.Margin = measurement.Margin;
        node.Padding = measurement.Padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.ApplyLayoutGeometry(BoxGeometryFactory.FromBorderBox(
            x,
            y,
            measurement.BorderBoxWidth,
            measurement.Padding.Vertical + measurement.Border.Vertical,
            measurement.Padding,
            measurement.Border,
            markerOffset: node.MarkerOffset));

        return node;
    }
    
    private float LayoutChildBlocks(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        var currentY = cursorY;
        var previousBottomMargin = 0f;

        for (var i = 0; i < parent.Children.Count; i++)
        {
            if (parent.Children[i] is not BlockBox childBlock)
            {
                continue;
            }

            var marginTop = childBlock.Style.Margin.Safe().Top;
            var collapsedTop = _blockFormattingContext.CollapseMargins(
                previousBottomMargin,
                marginTop,
                ResolveFormattingContext(parent),
                nameof(BlockLayoutEngine),
                _diagnosticsSession);
            var laidOutChild = LayoutBlock(
                childBlock,
                new BlockLayoutRequest(
                    contentX,
                    currentY,
                    contentWidth,
                    parentContentTop,
                    previousBottomMargin,
                    collapsedTop));
            parent.Children[i] = laidOutChild;
            currentY = laidOutChild.Y + laidOutChild.Height;
            previousBottomMargin = laidOutChild.Margin.Bottom;
        }

        return Math.Max(0, (currentY + previousBottomMargin) - cursorY);
    }
    internal TableBox LayoutTableBlock(TableBox node, BlockLayoutRequest request)
    {
        var s = node.Style;
        var margin = s.Margin.Safe();

        var origin = BlockPlacementService.ResolveOrigin(request, margin);
        var x = origin.X;
        var y = origin.Y;
        var result = _tableContext.LayoutTable(node, request.ContentWidth);
        if (!result.IsSupported)
        {
            TableLayoutDiagnostics.EmitUnsupportedTable(
                _diagnosticsSession,
                DisplayNodePathBuilder.Build(node),
                result.UnsupportedStructureKind ?? "unsupported-table-structure",
                result.UnsupportedReason ?? "Unsupported table structure.",
                result.RowCount,
                result.RequestedWidth,
                result.ResolvedWidth,
                groupContexts: BuildTableGroupContexts(node));

            return TablePlacementApplier.ApplyUnsupportedPlaceholder(node, x, y, result.ResolvedWidth, margin);
        }

        TableLayoutDiagnostics.EmitSupportedTable(
            _diagnosticsSession,
            DisplayNodePathBuilder.Build(node),
            result.Rows.Count,
            result.DerivedColumnCount,
            result.RequestedWidth,
            result.ResolvedWidth,
            BuildTableRowContexts(result),
            BuildTableCellContexts(result),
            BuildTableColumnContexts(result),
            BuildTableGroupContexts(node));

        return _tablePlacementApplier.ApplySupported(node, result, x, y, margin, LayoutChildBlocks);
    }

    private static IReadOnlyList<TableRowDiagnosticContext> BuildTableRowContexts(TableLayoutResult result)
    {
        return result.Rows
            .Select(static row => new TableRowDiagnosticContext(
                row.RowIndex,
                row.Cells.Count,
                row.Height))
            .ToList();
    }

    private static IReadOnlyList<TableCellDiagnosticContext> BuildTableCellContexts(TableLayoutResult result)
    {
        return result.Rows
            .SelectMany(static row => row.Cells.Select(cell => new TableCellDiagnosticContext(
                row.RowIndex,
                cell.ColumnIndex,
                cell.IsHeader,
                cell.Width,
                cell.Height)))
            .ToList();
    }

    private static IReadOnlyList<TableColumnDiagnosticContext> BuildTableColumnContexts(TableLayoutResult result)
    {
        return result.ColumnWidths
            .Select(static (width, index) => new TableColumnDiagnosticContext(index, width))
            .ToList();
    }

    private static IReadOnlyList<TableGroupDiagnosticContext> BuildTableGroupContexts(TableBox table)
    {
        var groups = table.Children
            .OfType<TableSectionBox>()
            .Select(static section => new TableGroupDiagnosticContext(
                section.Element?.TagName.ToLowerInvariant() ?? "section",
                section.Children.OfType<TableRowBox>().Count()))
            .ToList();

        if (groups.Count > 0)
        {
            return groups;
        }

        return
        [
            new TableGroupDiagnosticContext(
                "direct",
                table.Children.OfType<TableRowBox>().Count())
        ];
    }

    private static void CopyPageTo(PageBox target, PageBox source)
    {
        target.Size = source.Size;
        target.Margin = source.Margin;
    }

    private static float ResolveFlowContentHeight(float childBlockFlowHeight, float inlineFlowHeight)
    {
        return Math.Max(Math.Max(0f, childBlockFlowHeight), Math.Max(0f, inlineFlowHeight));
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox block) =>
        block.IsInlineBlockContext ? FormattingContextKind.InlineBlock : FormattingContextKind.Block;

}
