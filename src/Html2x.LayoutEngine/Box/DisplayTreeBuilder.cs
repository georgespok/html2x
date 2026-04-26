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
            DisplayRole.Block => CreateBlockLevelBox(display, element, styleNode.Style, parent),
            DisplayRole.Inline => new InlineBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.InlineBlock => new InlineBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.ListItem => CreateBlockLevelBox(display, element, styleNode.Style, parent),
            DisplayRole.Float => new FloatBox(display)
            {
                Element = element,
                Style = styleNode.Style,
                Parent = parent,
                FloatDirection = ResolveFloatDirection(styleNode)
            },
            DisplayRole.Table => new TableBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.TableSection => new TableSectionBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.TableRow => new TableRowBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
            DisplayRole.TableCell => new TableCellBox(display) { Element = element, Style = styleNode.Style, Parent = parent },
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

        TryInsertListMarker(element, parent, box);

        return box;
    }

    private static void TryInsertListMarker(IElement element, DisplayNode? parent, DisplayNode box)
    {
        if (!string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Li, StringComparison.OrdinalIgnoreCase) ||
            parent is null ||
            box is not BlockBox listItem)
        {
            return;
        }

        var markerText = ResolveListMarker(parent, box);
        if (string.IsNullOrWhiteSpace(markerText))
        {
            return;
        }

        listItem.MarkerOffset = HtmlCssConstants.Defaults.ListMarkerOffsetPt;
        var marker = new InlineBox(DisplayRole.Inline)
        {
            TextContent = markerText,
            Style = box.Style,
            Parent = box
        };

        box.Children.Insert(0, marker);
    }

    private static string ResolveListMarker(DisplayNode listContainer, DisplayNode listItem)
    {
        return ListMarkerResolver.ResolveMarkerText(listContainer, listItem);
    }

    private static BlockBox CreateInlineBlockContentContainer(InlineBox inlineBlock, StyleNode styleNode)
    {
        var contentBox = CreateBlockLevelBox(DisplayRole.Block, styleNode.Element, styleNode.Style, inlineBlock);
        contentBox.IsInlineBlockContext = true;
        contentBox.TextAlign = styleNode.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        inlineBlock.Children.Add(contentBox);
        return contentBox;
    }

    private static BlockBox CreateBlockLevelBox(
        DisplayRole role,
        IElement element,
        ComputedStyle style,
        DisplayNode? parent)
    {
        var isAnonymous = role == DisplayRole.Block && parent is InlineBox;

        return IsRuleElement(element)
            ? new RuleBox(role)
            {
                Element = element,
                Style = style,
                Parent = parent,
                IsAnonymous = isAnonymous
            }
            : IsImageElement(element)
                ? new ImageBox(role)
                {
                    Element = element,
                    Style = style,
                    Parent = parent,
                    IsAnonymous = isAnonymous,
                    Src = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? string.Empty
                }
                : new BlockBox(role)
                {
                    Element = element,
                    Style = style,
                    Parent = parent,
                    IsAnonymous = isAnonymous
                };
    }

    private static bool IsImageElement(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Img, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRuleElement(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Hr, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsListContainer(IElement element)
    {
        var tag = element.TagName;
        return string.Equals(tag, HtmlCssConstants.HtmlTags.Ul, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(tag, HtmlCssConstants.HtmlTags.Ol, StringComparison.OrdinalIgnoreCase);
    }

    private static DisplayRole GetDisplayRole(StyleNode node)
    {
        if (IsFloating(node.Style))
        {
            return DisplayRole.Float;
        }

        return DisplayRoleMap.Resolve(node.Style.Display, node.Element.LocalName);
    }

    private static bool IsFloating(ComputedStyle style)
    {
        return string.Equals(style.FloatDirection, HtmlCssConstants.CssValues.Left, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(style.FloatDirection, HtmlCssConstants.CssValues.Right, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveFloatDirection(StyleNode node)
    {
        return IsFloating(node.Style)
            ? node.Style.FloatDirection
            : HtmlCssConstants.Defaults.FloatDirection;
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
