using System;
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
        if (IsListContainer(element))
        {
            foreach (var child in styleNode.Children)
            {
                if (!string.Equals(child.Element.TagName, HtmlCssConstants.HtmlTags.Li,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                box.Children.Add(BuildNode(child, box));
            }
        }
        else
        {
            foreach (var child in styleNode.Children)
            {
                box.Children.Add(BuildNode(child, box));
            }
        }

        // Handle inline text content
        AppendTextRuns(element, box);

        if (string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Li, StringComparison.OrdinalIgnoreCase) &&
            parent != null)
        {
            var markerText = ResolveListMarker(parent, box);
            if (!string.IsNullOrWhiteSpace(markerText))
            {
                var marker = new InlineBox
                {
                    TextContent = markerText,
                    Style = box.Style,
                    Parent = box
                };

                box.Children.Insert(0, marker);
            }
        }

        return box;
    }

    private static string ResolveListMarker(DisplayNode listContainer, DisplayNode listItem)
    {
        var tag = listContainer.Element?.TagName.ToLowerInvariant();
        if (tag == HtmlCssConstants.HtmlTags.Ul)
        {
            return "• ";
        }

        if (tag == HtmlCssConstants.HtmlTags.Ol)
        {
            var index = listContainer.Children.IndexOf(listItem);
            return index >= 0 ? $"{index + 1}. " : "1. ";
        }

        return string.Empty;
    }

    private static bool IsListContainer(IElement element)
    {
        var tag = element.TagName;
        return string.Equals(tag, HtmlCssConstants.HtmlTags.Ul, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(tag, HtmlCssConstants.HtmlTags.Ol, StringComparison.OrdinalIgnoreCase);
    }

    private static DisplayRole GetDisplayRole(StyleNode node)
    {
        var el = node.Element;
        var name = el.LocalName.ToLowerInvariant();

        return name switch
        {
            HtmlCssConstants.HtmlTags.Table => DisplayRole.Table,

            HtmlCssConstants.HtmlTags.Tr => DisplayRole.TableRow,

            HtmlCssConstants.HtmlTags.Td or
                HtmlCssConstants.HtmlTags.Th => DisplayRole.TableCell,

            HtmlCssConstants.HtmlTags.Img when el.ClassList.Contains(HtmlCssConstants.CssClasses.Hero) => DisplayRole
                .Float,
            HtmlCssConstants.HtmlTags.Span => DisplayRole.Inline,

            HtmlCssConstants.HtmlTags.Ul or 
                HtmlCssConstants.HtmlTags.Ol => DisplayRole.Block,

            HtmlCssConstants.HtmlTags.Li => DisplayRole.Block,

            HtmlCssConstants.HtmlTags.Div or
                HtmlCssConstants.HtmlTags.P or
                HtmlCssConstants.HtmlTags.H1 or
                HtmlCssConstants.HtmlTags.H2 or
                HtmlCssConstants.HtmlTags.H3 or
                HtmlCssConstants.HtmlTags.H4 or
                HtmlCssConstants.HtmlTags.H5 or
                HtmlCssConstants.HtmlTags.H6 or
                HtmlCssConstants.HtmlTags.Body => DisplayRole.Block,

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
