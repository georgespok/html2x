using System;
using System.Collections.Generic;
using System;
using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Default implementation that walks the DOM for supported elements and
/// materializes a StyleNode tree using the provided style factory.
/// </summary>
public sealed class StyleTraversal : IStyleTraversal
{
    private readonly IStyleDomFilter _filter;

    public StyleTraversal()
        : this(new DefaultStyleDomFilter())
    {
    }

    public StyleTraversal(IStyleDomFilter filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public StyleNode Build(IElement root, Func<IElement, ComputedStyle?, ComputedStyle> styleFactory)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (styleFactory is null)
        {
            throw new ArgumentNullException(nameof(styleFactory));
        }

        return BuildNode(root, null, styleFactory);
    }

    private StyleNode BuildNode(IElement element, ComputedStyle? parent, Func<IElement, ComputedStyle?, ComputedStyle> styleFactory)
    {
        var style = styleFactory(element, parent);
        var node = new StyleNode
        {
            Element = element,
            Style = style
        };

        foreach (var child in element.Children)
        {
            if (!_filter.ShouldInclude(child))
            {
                continue;
            }

            var childNode = BuildNode(child, style, styleFactory);
            node.Children.Add(childNode);
        }

        return node;
    }
}
