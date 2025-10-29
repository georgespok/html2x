using AngleSharp.Dom;
using Html2x.Layout.Style;

namespace Html2x.Layout.Box;

public sealed class DisplayTreeBuilder
{
    public DisplayNode Build(StyleTree styleTree)
    {
        if (styleTree.Root is null)
        {
            throw new InvalidOperationException("Style tree has no root.");
        }

        // Create root box for <body>
        return BuildNode(styleTree.Root, null);
    }

    private static DisplayNode BuildNode(StyleNode styleNode, DisplayNode? parent)
    {
        var element = styleNode.Element;
        var display = GetDisplayRole(styleNode);

        DisplayNode box;

        switch (display)
        {
            case DisplayRole.Block:
                box = new BlockBox
                {
                    Element = element,
                    Style = styleNode.Style,
                    Parent = parent
                };
                break;

            case DisplayRole.Inline:
                box = new InlineBox
                {
                    Element = element,
                    Style = styleNode.Style,
                    Parent = parent
                };
                break;

            case DisplayRole.Float:
                box = new FloatBox
                {
                    Element = element,
                    Style = styleNode.Style,
                    Parent = parent,
                    FloatDirection = ResolveFloatDirection(styleNode)
                };
                break;

            case DisplayRole.Table:
                box = new TableBox
                {
                    Element = element,
                    Style = styleNode.Style,
                    Parent = parent
                };
                break;

            default:
                box = new BlockBox
                {
                    Element = element,
                    Style = styleNode.Style,
                    Parent = parent
                };
                break;
        }

        // Add child boxes recursively
        foreach (var child in styleNode.Children)
        {
            box.Children.Add(BuildNode(child, box));
        }

        // Handle inline text content
        AppendTextRuns(element, box);

        return box;
    }

    private static DisplayRole GetDisplayRole(StyleNode node)
    {
        var el = node.Element;
        var name = el.LocalName.ToLowerInvariant();

        return name switch
        {
            "table" => DisplayRole.Table,
            "tr" => DisplayRole.TableRow,
            "td" or "th" => DisplayRole.TableCell,
            "img" when el.ClassList.Contains("hero") => DisplayRole.Float,
            "span" => DisplayRole.Inline,
            "p" or "h1" or "body" => DisplayRole.Block,
            _ => DisplayRole.Inline
        };
    }

    private static string ResolveFloatDirection(StyleNode node)
    {
        // In future — derive from CSS property
        return node.Element.ClassList.Contains("hero")
            ? "right"
            : "left";
    }

    private static void AppendTextRuns(IElement element, DisplayNode box)
    {
        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == NodeType.Text)
            {
                var text = child.TextContent.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    box.Children.Add(new InlineBox
                    {
                        TextContent = text,
                        Parent = box,
                        Style = box.Style
                    });
                }
            }
        }
    }
}