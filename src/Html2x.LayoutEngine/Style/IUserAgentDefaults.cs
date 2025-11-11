using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Applies UA-style defaults to computed styles when author styles do not override them.
/// </summary>
public interface IUserAgentDefaults
{
    void Apply(IElement element, ComputedStyle style, ComputedStyle? inheritedStyle);
}
