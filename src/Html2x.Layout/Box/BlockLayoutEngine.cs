namespace Html2x.Layout.Box;

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

        var contentX = page.MarginLeftPt;
        var contentY = page.MarginTopPt;
        var contentWidth = page.PageWidthPt - page.MarginLeftPt - page.MarginRightPt;

        // iterate over block-level children only
        foreach (var child in displayRoot.Children)
        {
            if (child is InlineBox inline)
            {
                // Treat inline text runs that appear directly under the root (e.g. <body>text</body>)
                var block = LayoutInlineText(inline, contentX, contentY, contentWidth);
                tree.Blocks.Add(block);
                contentY = block.Y + block.Height + block.Margin.Bottom;
                continue;
            }

            if (child is not BlockBox && child is not TableBox)
            {
                // still skip floats or other unsupported types for now
                continue;
            }

            if (child is BlockBox box)
            {
                var block = LayoutBlock(box, contentX, contentY, contentWidth);
                tree.Blocks.Add(block);
                contentY = block.Y + block.Height + block.Margin.Bottom;
            }
            else if (child is TableBox table)
            {
                var tableBlock = LayoutTable(table, contentX, contentY, contentWidth);
                tree.Blocks.Add(tableBlock);
                contentY = tableBlock.Y + tableBlock.Height + tableBlock.Margin.Bottom;
            }
        }

        return tree;
    }

    private BlockBox LayoutInlineText(InlineBox inline, float contentX, float cursorY, float contentWidth)
    {
        var s = inline.Style;
        var margin = new Spacing
        {
            Top = Safe(s.MarginTopPt),
            Right = Safe(s.MarginRightPt),
            Bottom = Safe(s.MarginBottomPt),
            Left = Safe(s.MarginLeftPt)
        };

        var x = contentX + margin.Left;
        var y = cursorY + margin.Top;
        var width = Math.Max(0, contentWidth - margin.Left - margin.Right);

        // Use inline engine to estimate height
        var height = inlineEngine.MeasureHeight(inline, width);

        // Create a pseudo-block wrapper to represent this text run in BoxTree
        var block = new BlockBox
        {
            Element = inline.Element,
            Style = s,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Margin = margin,
            TextAlign = s.TextAlign ?? HtmlCssConstants.Defaults.TextAlign
        };

        return block;
    }


    private BlockBox LayoutBlock(BlockBox node, float contentX, float cursorY, float contentWidth)
    {
        var s = node.Style;
        var margin = new Spacing
        {
            Top = Safe(s.MarginTopPt),
            Right = Safe(s.MarginRightPt),
            Bottom = Safe(s.MarginBottomPt),
            Left = Safe(s.MarginLeftPt)
        };

        var x = contentX + margin.Left;
        var y = cursorY + margin.Top;
        var width = Math.Max(0, contentWidth - margin.Left - margin.Right);

        // use inline engine for height estimation
        floatEngine.PlaceFloats(node, x, y, width);
        var height = inlineEngine.MeasureHeight(node, width);

        node.X = x;
        node.Y = y;
        node.Width = width;
        node.Height = height;
        node.Margin = margin;
        node.TextAlign = s.TextAlign ?? "left";

        return node;
    }

    private BlockBox LayoutTable(TableBox node, float contentX, float cursorY, float contentWidth)
    {
        var s = node.Style;
        var margin = new Spacing
        {
            Top = Safe(s.MarginTopPt),
            Right = Safe(s.MarginRightPt),
            Bottom = Safe(s.MarginBottomPt),
            Left = Safe(s.MarginLeftPt)
        };

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

    private static float Safe(float v)
    {
        return float.IsFinite(v) ? v : 0f;
    }

    private static void CopyPageTo(PageBox target, PageBox source)
    {
        target.PageWidthPt = source.PageWidthPt;
        target.PageHeightPt = source.PageHeightPt;
        target.MarginTopPt = source.MarginTopPt;
        target.MarginRightPt = source.MarginRightPt;
        target.MarginBottomPt = source.MarginBottomPt;
        target.MarginLeftPt = source.MarginLeftPt;
    }
}