using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public sealed class PageStyle
{
    public Spacing Margin { get; set; } = new(24, 24, 24, 24);
}
