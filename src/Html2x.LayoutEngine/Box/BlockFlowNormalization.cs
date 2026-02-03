using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

internal static class BlockFlowNormalization
{
    public static void NormalizeChildrenForBlock(BlockBox parent)
    {
        if (parent.IsAnonymous)
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

        // Inline and block children cannot coexist directly; wrap inline sequences in anonymous blocks.
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
        var anon = new BlockBox(DisplayRole.Block)
        {
            IsAnonymous = true,
            Parent = parent,
            Element = parent.Element,
            Style = CreateAnonymousStyle(parent.Style),
            TextAlign = parent.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign
        };

        foreach (var inline in inlines)
        {
            var cloned = (InlineBox)CloneNode(inline, anon);
            anon.Children.Add(cloned);
        }

        return anon;
    }

    private static DisplayNode CloneNode(DisplayNode source, DisplayNode parent)
    {
        DisplayNode clone = source switch
        {
            InlineBox inline => new InlineBox(inline.Role)
            {
                TextContent = inline.TextContent,
                Element = inline.Element,
                Style = inline.Style,
                Parent = parent
            },
            BlockBox block => new BlockBox(block.Role)
            {
                Element = block.Element,
                Style = block.Style,
                Parent = parent,
                IsAnonymous = block.IsAnonymous,
                TextAlign = block.TextAlign,
                MarkerOffset = block.MarkerOffset,
                IsInlineBlockContext = block.IsInlineBlockContext
            },
            FloatBox floatBox => new FloatBox(floatBox.Role)
            {
                Element = floatBox.Element,
                Style = floatBox.Style,
                Parent = parent,
                FloatDirection = floatBox.FloatDirection
            },
            TableBox table => new TableBox(table.Role)
            {
                Element = table.Element,
                Style = table.Style,
                Parent = parent
            },
            TableSectionBox section => new TableSectionBox(section.Role)
            {
                Element = section.Element,
                Style = section.Style,
                Parent = parent
            },
            TableRowBox row => new TableRowBox(row.Role)
            {
                Element = row.Element,
                Style = row.Style,
                Parent = parent
            },
            TableCellBox cell => new TableCellBox(cell.Role)
            {
                Element = cell.Element,
                Style = cell.Style,
                Parent = parent
            },
            _ => new BlockBox(source.Role)
            {
                Element = source.Element,
                Style = source.Style,
                Parent = parent
            }
        };

        foreach (var child in source.Children)
        {
            var childClone = CloneNode(child, clone);
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
