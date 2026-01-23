using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public sealed class BlockLayoutEngine(
    IInlineLayoutEngine inlineEngine,
    ITableLayoutEngine tableEngine,
    IFloatLayoutEngine floatEngine,
    DiagnosticsSession? diagnosticsSession = null)
{
    private readonly DiagnosticsSession? _diagnosticsSession = diagnosticsSession;

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

        NormalizeChildrenForBlock(displayRoot);

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

        // use inline engine for height estimation (use content width)
        floatEngine.PlaceFloats(node, x, y, width); 
        var inlineHeight = inlineEngine.MeasureHeight(node, contentWidthForChildren);

        var nestedBlocksHeight = LayoutChildBlocks(
            node,
            contentXForChildren,
            contentYForChildren,
            contentWidthForChildren,
            contentYForChildren);
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

        NormalizeChildrenForBlock(parent);

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
        if (_diagnosticsSession is null)
        {
            return;
        }

        _diagnosticsSession.Events.Add(new DiagnosticsEvent
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
        var height = tableEngine.MeasureHeight(node, width);
        var size = new SizePt(width, height).Safe().ClampMin(0f, 0f);

        var box = new BlockBox
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
