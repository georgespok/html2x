using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public sealed class BlockLayoutEngine
{
    private readonly IInlineLayoutEngine _inlineEngine;
    private readonly ITableLayoutEngine _tableEngine;
    private readonly IFloatLayoutEngine _floatEngine;
    private readonly DiagnosticsSession? _diagnosticsSession;
    private readonly IBlockFormattingContext _blockFormattingContext;

    public BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        IFloatLayoutEngine floatEngine,
        DiagnosticsSession? diagnosticsSession = null)
        : this(inlineEngine, tableEngine, floatEngine, new BlockFormattingContext(), diagnosticsSession)
    {
    }

    internal BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        IFloatLayoutEngine floatEngine,
        IBlockFormattingContext blockFormattingContext,
        DiagnosticsSession? diagnosticsSession = null)
    {
        _inlineEngine = inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));
        _tableEngine = tableEngine ?? throw new ArgumentNullException(nameof(tableEngine));
        _floatEngine = floatEngine ?? throw new ArgumentNullException(nameof(floatEngine));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
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
            switch (child)
            {
                case BlockBox box:
                {
                    var marginTop = box.Style.Margin.Safe().Top;
                    var collapsedTop = Math.Max(previousBottomMargin, marginTop);
                    PublishMarginCollapse(previousBottomMargin, marginTop, collapsedTop);
                    var block = LayoutBlock(
                        box,
                        contentX,
                        currentY,
                        contentWidth,
                        contentY,
                        previousBottomMargin,
                        collapsedTop);
                    tree.Blocks.Add(block);
                    currentY = block.Y + block.Height;
                    previousBottomMargin = block.Margin.Bottom;
                    break;
                }
                case TableBox table:
                {
                    var marginTop = table.Style.Margin.Safe().Top;
                    var collapsedTop = Math.Max(previousBottomMargin, marginTop);
                    var tableBlock = LayoutTable(table, contentX, currentY, contentWidth, collapsedTop);
                    tree.Blocks.Add(tableBlock);
                    currentY = tableBlock.Y + tableBlock.Height;
                    previousBottomMargin = tableBlock.Margin.Bottom;
                    break;
                }
            }
        }

        return tree;
    }

    private static IReadOnlyList<DisplayNode> SelectTopLevelCandidates(DisplayNode displayRoot)
    {
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
        var hasBlockOrTable = block.Children.Any(c => c is BlockBox or TableBox);
        return hasInline && !hasBlockOrTable;
    }

    private BlockBox LayoutBlock(
        BlockBox node,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop,
        float previousBottomMargin,
        float collapsedTopMargin)
    {
        var s = node.Style;
        var margin = s.Margin.Safe();
        var padding = s.Padding.Safe();
        var border = Spacing.FromBorderEdges(s.Borders).Safe();

        var rawX = contentX + margin.Left;
        var rawY = cursorY + collapsedTopMargin;
        var x = Math.Max(rawX, contentX);
        var y = Math.Max(rawY, parentContentTop);
        
        var availableWidth = Math.Max(0, contentWidth - margin.Left - margin.Right);
        var width = s.WidthPt ?? availableWidth;

        if (s.MinWidthPt.HasValue)
        {
            width = Math.Max(width, s.MinWidthPt.Value);
        }

        if (s.MaxWidthPt.HasValue)
        {
            width = Math.Min(width, s.MaxWidthPt.Value);
        }

        // Content width accounts for padding and borders
        var contentWidthForChildren = Math.Max(0, width - padding.Horizontal - border.Horizontal);
        var contentXForChildren = x + padding.Left + border.Left;
        var contentYForChildren = y + padding.Top + border.Top;

        if (node.MarkerOffset > 0f)
        {
            contentXForChildren += node.MarkerOffset;
            contentWidthForChildren = Math.Max(0, contentWidthForChildren - node.MarkerOffset);
        }

        // use inline engine for height estimation (use content width)
        _floatEngine.PlaceFloats(node, x, y, width); 
        var inlineHeight = _inlineEngine.MeasureHeight(node, contentWidthForChildren);

        var nestedBlocksHeight = LayoutChildBlocks(
            node,
            contentXForChildren,
            contentYForChildren,
            contentWidthForChildren,
            contentYForChildren);
        var canonicalBlockHeight = ResolveCanonicalBlockHeight(node, contentWidthForChildren);
        var contentHeight = Math.Max(inlineHeight, Math.Max(nestedBlocksHeight, canonicalBlockHeight));
        if (s.HeightPt.HasValue)
        {
            contentHeight = s.HeightPt.Value;
        }

        if (s.MinHeightPt.HasValue)
        {
            contentHeight = Math.Max(contentHeight, s.MinHeightPt.Value);
        }

        if (s.MaxHeightPt.HasValue)
        {
            contentHeight = Math.Min(contentHeight, s.MaxHeightPt.Value);
        }

        var contentSize = new SizePt(width, contentHeight).Safe().ClampMin(0f, 0f);

        node.X = x;
        node.Y = y;
        node.Width = contentSize.Width; // Total width (for fragment Rect)
        node.Height = contentSize.Height + padding.Vertical + border.Vertical;
        node.Margin = margin;
        node.Padding = padding;
        node.TextAlign = s.TextAlign ?? "left";

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

        foreach (var child in parent.Children)
        {
            if (child is BlockBox blockChild)
            {
                var marginTop = blockChild.Style.Margin.Safe().Top;
                var collapsedTop = Math.Max(previousBottomMargin, marginTop);
                PublishMarginCollapse(previousBottomMargin, marginTop, collapsedTop);
                LayoutBlock(
                    blockChild,
                    contentX,
                    currentY,
                    contentWidth,
                    parentContentTop,
                    previousBottomMargin,
                    collapsedTop);
                currentY = blockChild.Y + blockChild.Height;
                previousBottomMargin = blockChild.Margin.Bottom;
            }
        }

        return Math.Max(0, (currentY + previousBottomMargin) - cursorY);
    }

    private void PublishMarginCollapse(float previousBottomMargin, float nextTopMargin, float collapsedTopMargin)
    {
        _diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = "layout/margin-collapse",
            Payload = new MarginCollapsePayload
            {
                PreviousBottomMargin = previousBottomMargin,
                NextTopMargin = nextTopMargin,
                CollapsedTopMargin = collapsedTopMargin
            }
        });
    }

    private BlockBox LayoutTable(TableBox node, float contentX, float cursorY, float contentWidth, float collapsedTopMargin)
    {
        var s = node.Style;
        var margin = s.Margin.Safe();

        var x = contentX + margin.Left;
        var y = cursorY + collapsedTopMargin;
        var width = Math.Max(0, contentWidth - margin.Left - margin.Right);
        var height = _tableEngine.MeasureHeight(node, width);
        var size = new SizePt(width, height).Safe().ClampMin(0f, 0f);

        var box = new BlockBox(DisplayRole.Block)
        {
            Element = node.Element,
            Style = s,
            X = x,
            Y = y,
            Width = size.Width,
            Height = size.Height,
            Margin = margin
        };

        return box;
    }

    private static void CopyPageTo(PageBox target, PageBox source)
    {
        target.Size = source.Size;
        target.Margin = source.Margin;
    }

    private float ResolveCanonicalBlockHeight(BlockBox node, float contentWidthForChildren)
    {
        if (!node.Children.OfType<BlockBox>().Any())
        {
            return 0f;
        }

        var request = float.IsFinite(contentWidthForChildren)
            ? BlockFormattingRequest.ForTopLevel(node, Math.Max(0f, contentWidthForChildren))
            : BlockFormattingRequest.ForUnboundedWidth(FormattingContextKind.Block, node);
        var result = _blockFormattingContext.Format(request);

        var formattedChildren = result.FormattedBlocks
            .Where(static block => block.Role == DisplayRole.Block)
            .Where(block => !ReferenceEquals(block, node))
            .ToList();

        if (formattedChildren.Count == 0)
        {
            return 0f;
        }

        var minY = float.PositiveInfinity;
        var maxY = float.NegativeInfinity;
        foreach (var block in formattedChildren)
        {
            var margin = block.Style.Margin.Safe();
            var top = block.Y - margin.Top;
            var bottom = block.Y + block.Height + margin.Bottom;
            minY = Math.Min(minY, top);
            maxY = Math.Max(maxY, bottom);
        }

        return Math.Max(0f, maxY - minY);
    }

}
