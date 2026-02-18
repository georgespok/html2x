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
            DisplayRole.Block => new BlockBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.Inline => new InlineBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.InlineBlock => new InlineBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.ListItem => new BlockBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.Float => new FloatBox(display)
            {
                Element = element,
                Style = styleNode.Style,
                Parent = parent,
                FloatDirection = ResolveFloatDirection(styleNode)
            },
            DisplayRole.Table => new TableBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            _ => new BlockBox(display) { Element = element, Style = styleNode.Style, Parent = parent }
        };

        // Add child boxes recursively
        var childContainer = box;
        var childState = textState;
        if (display == DisplayRole.InlineBlock && box is InlineBox inlineBlock)
        {
            var contentBox = CreateInlineBlockContentContainer(inlineBlock, styleNode);
            childContainer = contentBox;
            childState = textState.CreateForBlockBoundary();
        }

        AppendChildNodes(styleNode, childContainer, childState);

        if (childContainer is BlockBox childBlock)
        {
            BlockFlowNormalization.NormalizeChildrenForBlock(childBlock);
        }

        if (string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Li, StringComparison.OrdinalIgnoreCase) &&
            parent != null &&
            box is BlockBox listItem)
        {
            var markerText = ResolveListMarker(parent, box);
            if (!string.IsNullOrWhiteSpace(markerText))
            {
                listItem.MarkerOffset = HtmlCssConstants.Defaults.ListMarkerOffsetPt;
                var marker = new InlineBox(DisplayRole.Inline)
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

    private static BlockBox CreateInlineBlockContentContainer(InlineBox inlineBlock, StyleNode styleNode)
    {
        var contentBox = new BlockBox(DisplayRole.Block)
        {
            Element = styleNode.Element,
            Style = styleNode.Style,
            Parent = inlineBlock,
            IsAnonymous = true,
            TextAlign = styleNode.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign,
            IsInlineBlockContext = true
        };

        inlineBlock.Children.Add(contentBox);
        return contentBox;
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

        return DisplayRoleMap.Resolve(node.Style.Display, el.LocalName);
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
        var styleLookup = BuildStyleLookup(styleChildren);

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

            if (!styleLookup.TryGetValue(childElement, out var styleChild))
            {
                AppendUnsupportedElement(childElement, box, textState);
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

        box.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = normalized,
            Parent = box,
            Style = box.Style
        });
    }

    private static IReadOnlyDictionary<IElement, StyleNode> BuildStyleLookup(IReadOnlyList<StyleNode> children)
    {
        if (children.Count == 0)
        {
            return new Dictionary<IElement, StyleNode>();
        }

        var lookup = new Dictionary<IElement, StyleNode>(children.Count);
        foreach (var child in children)
        {
            if (child.Element is null)
            {
                continue;
            }

            if (!lookup.ContainsKey(child.Element))
            {
                lookup.Add(child.Element, child);
            }
        }

        return lookup;
    }

    private static void AppendUnsupportedElement(IElement element, DisplayNode box, TextNormalizationState textState)
    {
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

            AppendUnsupportedElement(childElement, box, textState);

            if (string.Equals(childElement.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase))
            {
                textState.MarkLineBreak();
            }
        }
    }
}
