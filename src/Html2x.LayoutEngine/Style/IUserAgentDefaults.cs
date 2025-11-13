using AngleSharp.Dom;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Applies UA-style defaults to computed styles when author styles do not override them.
/// </summary>
public interface IUserAgentDefaults
{
    void Apply(IElement element, ComputedStyle style, ComputedStyle? inheritedStyle);
}
