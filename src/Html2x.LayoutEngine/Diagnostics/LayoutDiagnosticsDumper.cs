using AngleSharp.Dom;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class LayoutDiagnosticsDumper
{
    public static StructuredDumpDocument Create(BoxTree boxTree)
    {
        if (boxTree is null)
        {
            throw new ArgumentNullException(nameof(boxTree));
        }

        var nodes = new List<StructuredDumpNode>(boxTree.Blocks.Count);
        var nodeCount = 0;

        for (var i = 0; i < boxTree.Blocks.Count; i++)
        {
            var block = boxTree.Blocks[i];
            nodes.Add(CreateNode(block, $"layout.{i}", ref nodeCount));
        }

        var summary = $"BoxTree nodes={nodeCount}";
        return new StructuredDumpDocument("dump/layout", summary, nodes, nodeCount);
    }

    private static StructuredDumpNode CreateNode(
        DisplayNode node,
        string nodeId,
        ref int nodeCount)
    {
        nodeCount++;

        var attributes = BuildAttributes(node);
        var children = new List<StructuredDumpNode>(node.Children.Count);

        for (var i = 0; i < node.Children.Count; i++)
        {
            children.Add(CreateNode(node.Children[i], $"{nodeId}.{i}", ref nodeCount));
        }

        return new StructuredDumpNode(
            nodeId,
            node.GetType().Name,
            ResolveName(node.Element),
            attributes,
            children);
    }

    private static IReadOnlyDictionary<string, object?> BuildAttributes(DisplayNode node)
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["childCount"] = node.Children.Count
        };

        if (!string.IsNullOrWhiteSpace(node.Element?.Id))
        {
            attributes["elementId"] = node.Element!.Id;
        }

        if (!string.IsNullOrWhiteSpace(node.Element?.ClassName))
        {
            attributes["class"] = node.Element!.ClassName;
        }

        attributes["fontFamily"] = node.Style.FontFamily;
        attributes["fontSizePt"] = node.Style.FontSizePt;

        switch (node)
        {
            case BlockBox block:
                attributes["x"] = block.X;
                attributes["y"] = block.Y;
                attributes["width"] = block.Width;
                attributes["height"] = block.Height;
                attributes["marginTop"] = block.Margin.Top;
                attributes["marginRight"] = block.Margin.Right;
                attributes["marginBottom"] = block.Margin.Bottom;
                attributes["marginLeft"] = block.Margin.Left;
                break;
            case InlineBox inlineBox:
                if (!string.IsNullOrWhiteSpace(inlineBox.TextContent))
                {
                    attributes["text"] = inlineBox.TextContent;
                }

                break;
            case FloatBox floatBox:
                attributes["float"] = floatBox.FloatDirection;
                break;
        }

        return attributes;
    }

    private static string? ResolveName(IElement? element)
    {
        if (element is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            return element.Id;
        }

        return element.LocalName;
    }
}
