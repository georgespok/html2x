using Html2x.LayoutEngine.Geometry.Style;

namespace Html2x.LayoutEngine.Geometry.Box;

internal sealed class BoxTreeConstruction
{
    public BoxNode Build(StyleTree styleTree)
    {
        if (styleTree.Root is null)
        {
            throw new InvalidOperationException("Style tree has no root.");
        }

        return BuildNode(styleTree.Root, null, new());
    }

    private BoxNode BuildNode(StyleNode styleNode, BoxNode? parent, TextNormalizationState textState)
    {
        var box = CreateBox(styleNode, parent);

        var childContainer = box;
        var childState = textState;
        if (box.Role == BoxRole.InlineBlock && box is InlineBox inlineBlock)
        {
            childContainer = CreateInlineBlockContentBox(inlineBlock, styleNode);
            childState = textState.CreateForBlockBoundary();
        }

        AddContent(styleNode, childContainer, childState);

        if (childContainer is BlockBox childBlock)
        {
            BlockFlowNormalization.NormalizeChildrenForBlock(childBlock);
        }

        AddListMarker(styleNode.Element, parent, box);

        return box;
    }

    private BoxNode CreateBox(StyleNode styleNode, BoxNode? parent)
    {
        ArgumentNullException.ThrowIfNull(styleNode);

        var role = ResolveRole(styleNode);
        var element = styleNode.Element;
        var style = styleNode.Style;
        var sourceIdentity = GeometrySourceIdentity.FromStyleNode(styleNode.Identity);

        return role switch
        {
            BoxRole.Block => CreateBlockLevelBox(role, element, style, parent, sourceIdentity),
            BoxRole.Inline => CreateInlineBox(role, element, style, parent, sourceIdentity),
            BoxRole.InlineBlock => CreateInlineBox(role, element, style, parent, sourceIdentity),
            BoxRole.ListItem => CreateBlockLevelBox(role, element, style, parent, sourceIdentity),
            BoxRole.Float => new FloatBox(role)
            {
                Element = element,
                Style = style,
                Parent = parent,
                SourceIdentity = sourceIdentity,
                FloatDirection = ResolveFloatDirection(style)
            },
            BoxRole.Table => new TableBox(role)
                { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            BoxRole.TableSection => new TableSectionBox(role)
                { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            BoxRole.TableRow => new TableRowBox(role)
                { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            BoxRole.TableCell => new TableCellBox(role)
                { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            _ => CreateBlockLevelBox(role, element, style, parent, sourceIdentity)
        };
    }

    private BlockBox CreateBlockLevelBox(
        BoxRole role,
        StyledElementFacts element,
        ComputedStyle style,
        BoxNode? parent,
        GeometrySourceIdentity sourceIdentity)
    {
        var isAnonymous = role == BoxRole.Block && parent is InlineBox;

        if (HtmlElementRules.IsRule(element))
        {
            return new RuleBox(role)
            {
                Element = element,
                Style = style,
                Parent = parent,
                IsAnonymous = isAnonymous,
                SourceIdentity = sourceIdentity
            };
        }

        if (HtmlElementRules.IsImage(element))
        {
            return new ImageBox(role)
            {
                Element = element,
                Style = style,
                Parent = parent,
                IsAnonymous = isAnonymous,
                SourceIdentity = sourceIdentity,
                Src = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? string.Empty
            };
        }

        return new(role)
        {
            Element = element,
            Style = style,
            Parent = parent,
            IsAnonymous = isAnonymous,
            SourceIdentity = sourceIdentity
        };
    }

    private static InlineBox CreateInlineBox(
        BoxRole role,
        StyledElementFacts element,
        ComputedStyle style,
        BoxNode? parent,
        GeometrySourceIdentity sourceIdentity) =>
        new(role)
        {
            Element = element,
            Style = style,
            Parent = parent,
            SourceIdentity = sourceIdentity
        };

    private BlockBox CreateInlineBlockContentBox(InlineBox inlineBlock, StyleNode styleNode)
    {
        ArgumentNullException.ThrowIfNull(inlineBlock);
        ArgumentNullException.ThrowIfNull(styleNode);

        var contentBox = CreateBlockLevelBox(
            BoxRole.Block,
            styleNode.Element,
            styleNode.Style,
            inlineBlock,
            GeometrySourceIdentity
                .FromStyleNode(styleNode.Identity)
                .AsGenerated(GeometryGeneratedSourceKind.InlineBlockContent));
        contentBox.IsInlineBlockContext = true;
        contentBox.TextAlign = styleNode.Style.TextAlign;

        inlineBlock.Children.Add(contentBox);
        return contentBox;
    }

    private void AddContent(
        StyleNode styleNode,
        BoxNode box,
        TextNormalizationState textState)
    {
        ArgumentNullException.ThrowIfNull(styleNode);
        ArgumentNullException.ThrowIfNull(box);
        ArgumentNullException.ThrowIfNull(textState);

        foreach (var content in EnumerateContent(styleNode))
        {
            switch (content.Kind)
            {
                case StyleContentNodeKind.Text:
                    AddTextRun(content, box, textState);
                    break;
                case StyleContentNodeKind.LineBreak:
                    textState.MarkLineBreak();
                    break;
                case StyleContentNodeKind.Element:
                    AddChildElement(styleNode, content.Element, box, textState);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported style content kind '{content.Kind}'.");
            }
        }
    }

    private void AddChildElement(
        StyleNode parentStyleNode,
        StyleNode? childStyleNode,
        BoxNode box,
        TextNormalizationState textState)
    {
        if (childStyleNode is null)
        {
            return;
        }

        if (HtmlElementRules.IsListContainer(parentStyleNode.Element) &&
            !HtmlElementRules.IsListItem(childStyleNode.Element))
        {
            return;
        }

        var role = ResolveRole(childStyleNode);
        var childState = role is BoxRole.Inline or BoxRole.InlineBlock
            ? textState
            : new();

        box.Children.Add(BuildNode(childStyleNode, box, childState));

        if (HtmlElementRules.IsLineBreak(childStyleNode.Element))
        {
            textState.MarkLineBreak();
        }
    }

    private static IEnumerable<StyleContentNode> EnumerateContent(StyleNode styleNode)
    {
        if (styleNode.Content.Count > 0)
        {
            return styleNode.Content;
        }

        return EnumerateChildContent(styleNode);
    }

    private static IEnumerable<StyleContentNode> EnumerateChildContent(StyleNode styleNode)
    {
        foreach (var child in styleNode.Children)
        {
            yield return StyleContentNode.ForElement(child);
        }
    }

    private static void AddTextRun(StyleContentNode content, BoxNode box, TextNormalizationState textState)
    {
        var text = content.Text ?? string.Empty;
        var normalized = StringNormalizer.NormalizeWhiteSpaceNormal(text, textState);
        if (string.IsNullOrEmpty(normalized))
        {
            return;
        }

        box.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = normalized,
            Parent = box,
            Style = box.Style,
            SourceIdentity = GeometrySourceIdentity.FromStyleContent(
                content.Identity,
                box.SourceIdentity.ElementIdentity,
                GeometryGeneratedSourceKind.AnonymousText)
        });
    }

    private static void AddListMarker(StyledElementFacts element, BoxNode? parent, BoxNode box)
    {
        if (!HtmlElementRules.IsListItem(element) ||
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

    private static BoxRole ResolveRole(StyleNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (IsFloating(node.Style))
        {
            return BoxRole.Float;
        }

        return BoxRoleMap.Resolve(node.Style.Display, node.Element.LocalName);
    }

    private static string ResolveFloatDirection(ComputedStyle style) =>
        IsFloating(style)
            ? style.FloatDirection
            : HtmlCssConstants.Defaults.FloatDirection;

    private static bool IsFloating(ComputedStyle style) =>
        string.Equals(style.FloatDirection, HtmlCssConstants.CssValues.Left, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(style.FloatDirection, HtmlCssConstants.CssValues.Right, StringComparison.OrdinalIgnoreCase);
}