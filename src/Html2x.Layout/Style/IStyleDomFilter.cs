using AngleSharp.Dom;

namespace Html2x.Layout.Style;

/// <summary>
/// Controls which DOM elements are included when building the style tree.
/// </summary>
public interface IStyleDomFilter
{
    bool ShouldInclude(IElement element);
}
