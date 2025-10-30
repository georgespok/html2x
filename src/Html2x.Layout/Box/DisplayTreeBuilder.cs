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
            HtmlCssConstants.HtmlTags.Table => DisplayRole.Table,
            HtmlCssConstants.HtmlTags.Tr => DisplayRole.TableRow,
            HtmlCssConstants.HtmlTags.Td or HtmlCssConstants.HtmlTags.Th => DisplayRole.TableCell,
            HtmlCssConstants.HtmlTags.Img when el.ClassList.Contains(HtmlCssConstants.CssClasses.Hero) => DisplayRole
                .Float,
            HtmlCssConstants.HtmlTags.Span => DisplayRole.Inline,
            HtmlCssConstants.HtmlTags.Div or HtmlCssConstants.HtmlTags.P or HtmlCssConstants.HtmlTags.H1 or HtmlCssConstants.HtmlTags.Body => DisplayRole
                .Block,
            _ => DisplayRole.Inline
        };
    }

    private static string ResolveFloatDirection(StyleNode node)
    {
        // In future — derive from CSS property
        return node.Element.ClassList.Contains(HtmlCssConstants.CssClasses.Hero)
            ? HtmlCssConstants.CssValues.Right
            : HtmlCssConstants.CssValues.Left;
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