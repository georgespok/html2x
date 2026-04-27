using AngleSharp.Dom;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine.Box;

public sealed class InitialBoxTreeBuilder
{
    public BoxNode Build(StyleTree styleTree)
    {
        if (styleTree.Root is null)
        {
            throw new InvalidOperationException("Style tree has no root.");
        }

        // Create root box for <body>
        return BuildNode(styleTree.Root, null, new TextNormalizationState());
    }

    private static BoxNode BuildNode(StyleNode styleNode, BoxNode? parent, TextNormalizationState textState)
    {
        var element = styleNode.Element;
        var box = CreateBox(styleNode, parent);

        // Add child boxes recursively
        var childContainer = box;
        var childState = textState;
        if (box.Role == BoxRole.InlineBlock && box is InlineBox inlineBlock)
        {
            var contentBox = CreateInlineBlockContentBox(inlineBlock, styleNode);
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

    private static void TryInsertListMarker(IElement element, BoxNode? parent, BoxNode box)
    {
        if (!HtmlElementClassifier.IsListItem(element) ||
            parent is null ||
            box is not BlockBox listItem)
        {
            return;
        }

        var marker = ListMarkerPolicy.CreateMarker(parent, listItem);
        if (marker is null)
        {
            return;
        }

        listItem.MarkerOffset = HtmlCssConstants.Defaults.ListMarkerOffsetPt;
        box.Children.Insert(0, marker);
    }

    private static void AppendChildNodes(StyleNode styleNode, BoxNode box, TextNormalizationState textState)
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

            if (HtmlElementClassifier.IsListContainer(element) &&
                !HtmlElementClassifier.IsListItem(childElement))
            {
                continue;
            }

            if (!styleLookup.TryGetValue(childElement, out var styleChild))
            {
                AppendUnsupportedElement(childElement, box, textState);
                continue;
            }

            var role = ResolveBoxRole(styleChild);
            var childState = role is BoxRole.Inline or BoxRole.InlineBlock
                ? textState
                : new TextNormalizationState();

            box.Children.Add(BuildNode(styleChild, box, childState));

            if (HtmlElementClassifier.IsLineBreak(childElement))
            {
                textState.MarkLineBreak();
            }
        }
    }

    private static void AppendTextRun(INode node, BoxNode box, TextNormalizationState textState)
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

        box.Children.Add(new InlineBox(BoxRole.Inline)
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

    private static void AppendUnsupportedElement(IElement element, BoxNode box, TextNormalizationState textState)
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

            if (HtmlElementClassifier.IsLineBreak(childElement))
            {
                textState.MarkLineBreak();
            }
        }
    }

    private static BoxNode CreateBox(StyleNode styleNode, BoxNode? parent)
    {
        ArgumentNullException.ThrowIfNull(styleNode);

        var role = ResolveBoxRole(styleNode);
        var element = styleNode.Element;
        var style = styleNode.Style;

        return role switch
        {
            BoxRole.Block => CreateBlockLevelBox(role, element, style, parent),
            BoxRole.Inline => CreateInlineBox(role, element, style, parent),
            BoxRole.InlineBlock => CreateInlineBox(role, element, style, parent),
            BoxRole.ListItem => CreateBlockLevelBox(role, element, style, parent),
            BoxRole.Float => new FloatBox(role)
            {
                Element = element,
                Style = style,
                Parent = parent,
                FloatDirection = ResolveFloatDirection(style)
            },
            BoxRole.Table => new TableBox(role) { Element = element, Style = style, Parent = parent },
            BoxRole.TableSection => new TableSectionBox(role) { Element = element, Style = style, Parent = parent },
            BoxRole.TableRow => new TableRowBox(role) { Element = element, Style = style, Parent = parent },
            BoxRole.TableCell => new TableCellBox(role) { Element = element, Style = style, Parent = parent },
            _ => CreateBlockLevelBox(role, element, style, parent)
        };
    }

    private static BlockBox CreateInlineBlockContentBox(InlineBox inlineBlock, StyleNode styleNode)
    {
        ArgumentNullException.ThrowIfNull(inlineBlock);
        ArgumentNullException.ThrowIfNull(styleNode);

        var contentBox = CreateBlockLevelBox(
            BoxRole.Block,
            styleNode.Element,
            styleNode.Style,
            inlineBlock);
        contentBox.IsInlineBlockContext = true;
        contentBox.TextAlign = styleNode.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        inlineBlock.Children.Add(contentBox);
        return contentBox;
    }

    private static BoxRole ResolveBoxRole(StyleNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (IsFloating(node.Style))
        {
            return BoxRole.Float;
        }

        return BoxRoleMap.Resolve(node.Style.Display, node.Element.LocalName);
    }

    private static InlineBox CreateInlineBox(
        BoxRole role,
        IElement element,
        ComputedStyle style,
        BoxNode? parent)
    {
        return new InlineBox(role)
        {
            Element = element,
            Style = style,
            Parent = parent
        };
    }

    private static BlockBox CreateBlockLevelBox(
        BoxRole role,
        IElement element,
        ComputedStyle style,
        BoxNode? parent)
    {
        var isAnonymous = role == BoxRole.Block && parent is InlineBox;

        if (HtmlElementClassifier.IsRule(element))
        {
            return new RuleBox(role)
            {
                Element = element,
                Style = style,
                Parent = parent,
                IsAnonymous = isAnonymous
            };
        }

        if (HtmlElementClassifier.IsImage(element))
        {
            return new ImageBox(role)
            {
                Element = element,
                Style = style,
                Parent = parent,
                IsAnonymous = isAnonymous,
                Src = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? string.Empty
            };
        }

        return new BlockBox(role)
        {
            Element = element,
            Style = style,
            Parent = parent,
            IsAnonymous = isAnonymous
        };
    }

    private static bool IsFloating(ComputedStyle style)
    {
        return string.Equals(style.FloatDirection, HtmlCssConstants.CssValues.Left, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(style.FloatDirection, HtmlCssConstants.CssValues.Right, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveFloatDirection(ComputedStyle style)
    {
        return IsFloating(style)
            ? style.FloatDirection
            : HtmlCssConstants.Defaults.FloatDirection;
    }
}
