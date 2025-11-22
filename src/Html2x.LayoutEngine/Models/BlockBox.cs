using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public sealed class BlockBox : DisplayNode
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Spacing Margin { get; set; } = new();
    public Spacing Padding { get; set; } = new();
    public string TextAlign { get; set; } = HtmlCssConstants.Defaults.TextAlign;
    public bool IsAnonymous { get; init; }
}
