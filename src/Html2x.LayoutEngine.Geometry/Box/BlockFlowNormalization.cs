using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;

internal static class BlockFlowNormalization
{
    public static void NormalizeChildrenForBlock(BlockBox parent)
    {
        if (parent.IsAnonymous && !parent.IsInlineBlockContext)
        {
            return;
        }

        var hasInline = parent.Children.Any(IsInlineFlowNode);
        var hasNonInline = parent.Children.Any(c => !IsInlineFlowNode(c));

        // Only create anonymous blocks when inline and block-level nodes coexist.
        if (!hasInline || !hasNonInline)
        {
            return;
        }

        // Invariant: the normalized child sequence becomes the canonical sibling order for later stages.
        // Fragment construction must emit children in this same order without reordering.
        // For mixed inline text around an inline-block boundary, the expected sequence is:
        // anonymous inline-text block, explicit inline-block boundary placeholder, anonymous trailing inline-text block.
        // Inline and block children cannot coexist directly; wrap inline sequences in anonymous blocks.
        var normalized = new List<BoxNode>(parent.Children.Count);
        var inlineBuffer = new List<InlineBox>();

        foreach (var child in parent.Children)
        {
            if (TryCreateInlineBlockBoundaryNode(parent, child, out var boundaryNode))
            {
                if (inlineBuffer.Count > 0)
                {
                    normalized.Add(CreateAnonymousBlock(parent, inlineBuffer));
                    inlineBuffer.Clear();
                }

                normalized.Add(boundaryNode);
                continue;
            }

            if (IsInlineFlowNode(child) && child is InlineBox inline)
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

    private static bool TryCreateInlineBlockBoundaryNode(BlockBox parent, BoxNode child, out BoxNode boundaryNode)
    {
        boundaryNode = null!;

        if (!parent.IsInlineBlockContext ||
            child is not InlineBox inlineBlock ||
            inlineBlock.Role != BoxRole.InlineBlock)
        {
            return false;
        }

        var inlineBlockContent = inlineBlock.Children.OfType<BlockBox>().FirstOrDefault();
        if (inlineBlockContent is null)
        {
            return false;
        }

        boundaryNode = CreateInlineBlockBoundaryNode(parent, inlineBlock, inlineBlockContent);
        return true;
    }

    private static bool IsInlineFlowNode(BoxNode node)
    {
        if (node is not InlineBox inline || inline.Role != BoxRole.Inline)
        {
            return false;
        }

        // Inline nodes that already carry block-level descendants must act as block boundaries.
        return !inline.Children.Any(static child => child is BlockBox);
    }

    private static BlockBox CreateAnonymousBlock(BoxNode parent, List<InlineBox> inlines)
    {
        var anon = new BlockBox(BoxRole.Block)
        {
            IsAnonymous = true,
            Parent = parent,
            Element = parent.Element,
            Style = CreateAnonymousStyle(parent.Style),
            TextAlign = parent.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign,
            SourceIdentity = GeometrySourceIdentity
                .FirstSpecified(inlines[0].SourceIdentity, parent.SourceIdentity)
                .AsGenerated(GeometryGeneratedSourceKind.AnonymousBlock)
        };

        foreach (var inline in inlines)
        {
            var cloned = (InlineBox)inline.CloneForParent(anon);
            anon.Children.Add(cloned);
        }

        return anon;
    }

    private static InlineBlockBoundaryBox CreateInlineBlockBoundaryNode(
        BoxNode parent,
        InlineBox sourceInline,
        BlockBox sourceContentBox)
    {
        var boundary = new InlineBlockBoundaryBox(sourceInline, sourceContentBox)
        {
            Element = sourceContentBox.Element,
            Style = sourceContentBox.Style,
            Parent = parent,
            IsAnonymous = sourceContentBox.IsAnonymous,
            TextAlign = sourceContentBox.TextAlign,
            MarkerOffset = sourceContentBox.MarkerOffset,
            UsedGeometry = sourceContentBox.UsedGeometry,
            IsInlineBlockContext = sourceContentBox.IsInlineBlockContext,
            SourceIdentity = GeometrySourceIdentity
                .FirstSpecified(sourceContentBox.SourceIdentity, sourceInline.SourceIdentity)
                .AsGenerated(GeometryGeneratedSourceKind.InlineBlockBoundary)
        };

        foreach (var child in sourceContentBox.Children)
        {
            boundary.Children.Add(child.CloneForParent(boundary));
        }

        return boundary;
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
