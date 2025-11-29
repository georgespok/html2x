using AngleSharp.Dom;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine.Box;

public sealed class DisplayTreeBuilder
{
    public DisplayNode Build(StyleTree styleTree)
    {
        if (styleTree.Root is null)
        {
            throw new InvalidOperationException("Style tree has no root.");
        }

        // Create root box for <body>
        return BuildNode(styleTree.Root, null, new TextNormalizationState());
    }

    private static DisplayNode BuildNode(StyleNode styleNode, DisplayNode? parent, TextNormalizationState textState)
    {
        var element = styleNode.Element;
        var display = GetDisplayRole(styleNode);

        DisplayNode box = display switch
        {
            DisplayRole.Block => new BlockBox { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.Inline => new InlineBox { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.InlineBlock => new InlineBox { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.ListItem => new BlockBox { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.Float => new FloatBox
            {
                Element = element,
                Style = styleNode.Style,
                Parent = parent,
                FloatDirection = ResolveFloatDirection(styleNode)
            },
            DisplayRole.Table => new TableBox { Element = element, Style = styleNode.Style, Parent = parent },
            _ => new BlockBox { Element = element, Style = styleNode.Style, Parent = parent }
        };

        // Add child boxes recursively
        AppendChildNodes(styleNode, box, textState);

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
        return tag switch
        {
            HtmlCssConstants.HtmlTags.Ul => "• ",
            HtmlCssConstants.HtmlTags.Ol => $"{listContainer.Children.Count + 1}. ",
            _ => string.Empty
        };
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
        if (string.Equals(el.TagName, HtmlCssConstants.HtmlTags.Img, StringComparison.OrdinalIgnoreCase) &&
            el.ClassList.Contains(HtmlCssConstants.CssClasses.Hero))
        {
            return DisplayRole.Float;
        }

        return DisplayRoleMap.Resolve(el.LocalName);
    }

    private static string ResolveFloatDirection(StyleNode node)
    {
        // In future — derive from CSS property
        return node.Element.ClassList.Contains(HtmlCssConstants.CssClasses.Hero)
            ? HtmlCssConstants.CssValues.Right
            : HtmlCssConstants.CssValues.Left;
    }

    private static void AppendChildNodes(StyleNode styleNode, DisplayNode box, TextNormalizationState textState)
    {
        var element = styleNode.Element;
        var styleChildren = styleNode.Children;
        var styleIndex = 0;

        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == NodeType.Text)
            {
                AppendTextRun(child, box, textState);
                continue;
            }

            if (child is not IElement childElement)
            {
                continue;
            }

            if (IsListContainer(element) &&
                !string.Equals(childElement.TagName, HtmlCssConstants.HtmlTags.Li, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var styleChild = FindStyleChild(styleChildren, ref styleIndex, childElement);
            if (styleChild is null)
            {
                continue;
            }

            var role = GetDisplayRole(styleChild);
            var childState = role is DisplayRole.Inline or DisplayRole.InlineBlock
                ? textState
                : new TextNormalizationState();

            box.Children.Add(BuildNode(styleChild, box, childState));

            if (string.Equals(childElement.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase))
            {
                textState.MarkLineBreak();
            }
        }
    }

    private static void AppendTextRun(INode node, DisplayNode box, TextNormalizationState textState)
    {
        if (node.NodeType != NodeType.Text)
        {
            return;
        }

        var normalized = StringNormalizer.NormalizeWhiteSpaceNormal(node.TextContent, textState);
        if (string.IsNullOrEmpty(normalized))
        {
            return;
        }

        box.Children.Add(new InlineBox
        {
            TextContent = normalized,
            Parent = box,
            Style = box.Style
        });
    }

    private static StyleNode? FindStyleChild(IReadOnlyList<StyleNode> children, ref int styleIndex, IElement element)
    {
        for (; styleIndex < children.Count; styleIndex++)
        {
            var candidate = children[styleIndex];
            if (!ReferenceEquals(candidate.Element, element))
            {
                continue;
            }

            styleIndex++;
            return candidate;
        }

        return null;
    }
}
