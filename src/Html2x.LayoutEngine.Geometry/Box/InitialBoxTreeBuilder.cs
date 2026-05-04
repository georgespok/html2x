using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;

internal sealed class InitialBoxTreeBuilder
{
    public BoxNode Build(StyleTree styleTree)
    {
        if (styleTree.Root is null)
        {
            throw new InvalidOperationException("Style tree has no root.");
        }

        return BuildNode(styleTree.Root, null, new TextNormalizationState());
    }

    private static BoxNode BuildNode(StyleNode styleNode, BoxNode? parent, TextNormalizationState textState)
    {
        var element = styleNode.Element;
        var box = CreateBox(styleNode, parent);

        var childContainer = box;
        var childState = textState;
        if (box.Role == BoxRole.InlineBlock && box is InlineBox inlineBlock)
        {
            var contentBox = CreateInlineBlockContentBox(inlineBlock, styleNode);
            childContainer = contentBox;
            childState = textState.CreateForBlockBoundary();
        }

        AppendContentNodes(styleNode, childContainer, childState);

        if (childContainer is BlockBox childBlock)
        {
            BlockFlowNormalization.NormalizeChildrenForBlock(childBlock);
        }

        TryInsertListMarker(element, parent, box);

        return box;
    }

    private static void TryInsertListMarker(StyledElementFacts element, BoxNode? parent, BoxNode box)
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

    private static void AppendContentNodes(StyleNode styleNode, BoxNode box, TextNormalizationState textState)
    {
        foreach (var content in EnumerateContent(styleNode))
        {
            switch (content.Kind)
            {
                case StyleContentNodeKind.Text:
                    AppendTextRun(content, box, textState);
                    break;
                case StyleContentNodeKind.LineBreak:
                    textState.MarkLineBreak();
                    break;
                case StyleContentNodeKind.Element:
                    AppendChildElement(styleNode, content.Element, box, textState);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported style content kind '{content.Kind}'.");
            }
        }
    }

    private static void AppendChildElement(
        StyleNode parentStyleNode,
        StyleNode? childStyleNode,
        BoxNode box,
        TextNormalizationState textState)
    {
        if (childStyleNode is null)
        {
            return;
        }

        if (HtmlElementClassifier.IsListContainer(parentStyleNode.Element) &&
            !HtmlElementClassifier.IsListItem(childStyleNode.Element))
        {
            return;
        }

        var role = ResolveBoxRole(childStyleNode);
        var childState = role is BoxRole.Inline or BoxRole.InlineBlock
            ? textState
            : new TextNormalizationState();

        box.Children.Add(BuildNode(childStyleNode, box, childState));

        if (HtmlElementClassifier.IsLineBreak(childStyleNode.Element))
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

    private static void AppendTextRun(StyleContentNode content, BoxNode box, TextNormalizationState textState)
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

    private static BoxNode CreateBox(StyleNode styleNode, BoxNode? parent)
    {
        ArgumentNullException.ThrowIfNull(styleNode);

        var role = ResolveBoxRole(styleNode);
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
            BoxRole.Table => new TableBox(role) { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            BoxRole.TableSection => new TableSectionBox(role) { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            BoxRole.TableRow => new TableRowBox(role) { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            BoxRole.TableCell => new TableCellBox(role) { Element = element, Style = style, Parent = parent, SourceIdentity = sourceIdentity },
            _ => CreateBlockLevelBox(role, element, style, parent, sourceIdentity)
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
            inlineBlock,
            GeometrySourceIdentity
                .FromStyleNode(styleNode.Identity)
                .AsGenerated(GeometryGeneratedSourceKind.InlineBlockContent));
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
        StyledElementFacts element,
        ComputedStyle style,
        BoxNode? parent,
        GeometrySourceIdentity sourceIdentity)
    {
        return new InlineBox(role)
        {
            Element = element,
            Style = style,
            Parent = parent,
            SourceIdentity = sourceIdentity
        };
    }

    private static BlockBox CreateBlockLevelBox(
        BoxRole role,
        StyledElementFacts element,
        ComputedStyle style,
        BoxNode? parent,
        GeometrySourceIdentity sourceIdentity)
    {
        var isAnonymous = role == BoxRole.Block && parent is InlineBox;

        if (HtmlElementClassifier.IsRule(element))
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

        if (HtmlElementClassifier.IsImage(element))
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

        return new BlockBox(role)
        {
            Element = element,
            Style = style,
            Parent = parent,
            IsAnonymous = isAnonymous,
            SourceIdentity = sourceIdentity
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
