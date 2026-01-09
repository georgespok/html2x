using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Utilities;

namespace Html2x.LayoutEngine.Box;

public sealed class BlockLayoutEngine(
    IInlineLayoutEngine inlineEngine,
    ITableLayoutEngine tableEngine,
    IFloatLayoutEngine floatEngine)
{
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
        var contentWidth = page.PageWidthPt - page.Margin.Left - page.Margin.Right;

        NormalizeChildrenForBlock(displayRoot);

        var candidates = SelectTopLevelCandidates(displayRoot);

        foreach (var child in candidates)
        {
            switch (child)
            {
                case BlockBox box:
                {
                    var block = LayoutBlock(box, contentX, contentY, contentWidth);
                    tree.Blocks.Add(block);
                    contentY = block.Y + block.Height + block.Margin.Bottom;
                    break;
                }
                case TableBox table:
                {
                    var tableBlock = LayoutTable(table, contentX, contentY, contentWidth);
                    tree.Blocks.Add(tableBlock);
                    contentY = tableBlock.Y + tableBlock.Height + tableBlock.Margin.Bottom;
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
            var hasInline = rootBlock.Children.Any(c => c is InlineBox);
            var hasBlockOrTable = rootBlock.Children.Any(c => c is BlockBox or TableBox);

            // If the root contains only inline content, treat the root itself as the block to layout
            // so inline-only documents still render without introducing anonymous blocks.
            if (hasInline && !hasBlockOrTable)
            {
                return [rootBlock];
            }

            return rootBlock.Children;
        }

        return [displayRoot];
    }

    private BlockBox LayoutBlock(BlockBox node, float contentX, float cursorY, float contentWidth)
    {
        var s = node.Style;
        var margin = new Spacing(
            LayoutMath.Safe(s.Margin.Top),
            LayoutMath.Safe(s.Margin.Right),
            LayoutMath.Safe(s.Margin.Bottom),
            LayoutMath.Safe(s.Margin.Left));

        var padding = new Spacing(
            LayoutMath.Safe(s.Padding.Top),
            LayoutMath.Safe(s.Padding.Right),
            LayoutMath.Safe(s.Padding.Bottom),
            LayoutMath.Safe(s.Padding.Left));

        var borderTop = LayoutMath.Safe(s.Borders?.Top?.Width ?? 0);
        var borderRight = LayoutMath.Safe(s.Borders?.Right?.Width ?? 0);
        var borderBottom = LayoutMath.Safe(s.Borders?.Bottom?.Width ?? 0);
        var borderLeft = LayoutMath.Safe(s.Borders?.Left?.Width ?? 0);

        var x = contentX + margin.Left;
        var y = cursorY + margin.Top;
        
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
        var contentWidthForChildren = Math.Max(0, width - padding.Left - padding.Right - borderLeft - borderRight);
        var contentXForChildren = x + padding.Left + borderLeft;
        var contentYForChildren = y + padding.Top + borderTop;

        // use inline engine for height estimation (use content width)
        floatEngine.PlaceFloats(node, x, y, width); 
        var inlineHeight = inlineEngine.MeasureHeight(node, contentWidthForChildren);

        var nestedBlocksHeight = LayoutChildBlocks(node, contentXForChildren, contentYForChildren, contentWidthForChildren);
        var contentHeight = Math.Max(inlineHeight, nestedBlocksHeight);
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

        node.X = x;
        node.Y = y;
        node.Width = width; // Total width (for fragment Rect)
        node.Height = contentHeight + padding.Top + padding.Bottom + borderTop + borderBottom;
        node.Margin = margin;
        node.Padding = padding;
        node.TextAlign = s.TextAlign ?? "left";

        return node;
    }
    
    private float LayoutChildBlocks(BlockBox parent, float contentX, float cursorY, float contentWidth)
    {
        var currentY = cursorY;

        NormalizeChildrenForBlock(parent);

        foreach (var child in parent.Children)
        {
            if (child is BlockBox blockChild)
            {
                LayoutBlock(blockChild, contentX, currentY, contentWidth);
                currentY = blockChild.Y + blockChild.Height + blockChild.Margin.Bottom;
            }
        }

        return Math.Max(0, currentY - cursorY);
    }

    private BlockBox LayoutTable(TableBox node, float contentX, float cursorY, float contentWidth)
    {
        var s = node.Style;
        var margin = new Spacing(
            LayoutMath.Safe(s.Margin.Top),
            LayoutMath.Safe(s.Margin.Right),
            LayoutMath.Safe(s.Margin.Bottom),
            LayoutMath.Safe(s.Margin.Left));

        var x = contentX + margin.Left;
        var y = cursorY + margin.Top;
        var width = Math.Max(0, contentWidth - margin.Left - margin.Right);

        var height = tableEngine.MeasureHeight(node, width);

        var box = new BlockBox
        {
            Element = node.Element,
            Style = s,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Margin = margin
        };

        return box;
    }

    private static void CopyPageTo(PageBox target, PageBox source)
    {
        target.PageWidthPt = source.PageWidthPt;
        target.PageHeightPt = source.PageHeightPt;
        target.Margin = source.Margin;
    }

    private static void NormalizeChildrenForBlock(DisplayNode parent)
    {
        if (parent is BlockBox { IsAnonymous: true })
        {
            return;
        }

        var hasInline = parent.Children.Any(c => c is InlineBox);
        var hasNonInline = parent.Children.Any(c => c is not InlineBox);

        // Only create anonymous blocks when inline and block-level nodes coexist.
        if (!hasInline || !hasNonInline)
        {
            return;
        }

        var normalized = new List<DisplayNode>(parent.Children.Count);
        var inlineBuffer = new List<InlineBox>();

        foreach (var child in parent.Children)
        {
            if (child is InlineBox inline)
            {
                inlineBuffer.Add(inline);
                continue;
            }

            if (inlineBuffer.Count > 0)
            {
                normalized.Add(CreateAnonymousBlock(parent, inlineBuffer));
                inlineBuffer.Clear();
            }

            normalized.Add(child);
        }

        if (inlineBuffer.Count > 0)
        {
            normalized.Add(CreateAnonymousBlock(parent, inlineBuffer));
        }

        parent.Children.Clear();
        parent.Children.AddRange(normalized);
    }

    private static BlockBox CreateAnonymousBlock(DisplayNode parent, List<InlineBox> inlines)
    {
        var anon = new BlockBox
        {
            IsAnonymous = true,
            Parent = parent,
            Element = parent.Element,
            Style = CreateAnonymousStyle(parent.Style),
            TextAlign = parent.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign
        };

        foreach (var inline in inlines)
        {
            var cloned = CloneInline(inline, anon);
            anon.Children.Add(cloned);
        }

        return anon;
    }

    private static InlineBox CloneInline(InlineBox source, DisplayNode parent)
    {
        var clone = new InlineBox
        {
            TextContent = source.TextContent,
            Element = source.Element,
            Style = source.Style,
            Parent = parent
        };

        foreach (var child in source.Children.OfType<InlineBox>())
        {
            var childClone = CloneInline(child, clone);
            clone.Children.Add(childClone);
        }

        return clone;
    }

    private static ComputedStyle CreateAnonymousStyle(ComputedStyle parentStyle)
    {
        return new ComputedStyle
        {
            FontFamily = parentStyle.FontFamily,
            FontSizePt = parentStyle.FontSizePt,
            Bold = parentStyle.Bold,
            Italic = parentStyle.Italic,
            TextAlign = parentStyle.TextAlign,
            Color = parentStyle.Color,
            BackgroundColor = parentStyle.BackgroundColor,
            Margin = new Spacing(0, 0, 0, 0),
            Padding = new Spacing(0, 0, 0, 0),
            WidthPt = null,
            MinWidthPt = null,
            MaxWidthPt = null,
            HeightPt = null,
            MinHeightPt = null,
            MaxHeightPt = null,
            Borders = parentStyle.Borders
        };
    }
}
