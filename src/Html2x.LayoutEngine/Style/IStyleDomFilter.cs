using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Controls which DOM elements are included when building the style tree.
/// </summary>
public interface IStyleDomFilter
{
    bool ShouldInclude(IElement element);
}
