using System;
using AngleSharp.Dom;

namespace Html2x.Layout.Style;

/// <summary>
/// Traverses the DOM and produces a StyleNode tree using the supplied style factory.
/// </summary>
public interface IStyleTraversal
{
    StyleNode Build(IElement root, Func<IElement, ComputedStyle?, ComputedStyle> styleFactory);
}
