using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Contracts.Style;

internal sealed class PageStyle
{
    public Spacing Margin { get; set; } = new(24, 24, 24, 24);
}