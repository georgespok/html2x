using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Models;

public sealed class InlineBox(DisplayRole role) : DisplayNode(role)
{
    public string? TextContent { get; init; } // For inline text nodes

    public float Width { get; set; }
    public float Height { get; set; }
    public SizePt Size
    {
        get => new(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }
    public float BaselineOffset { get; set; }

    public object? Fragment { get; set; } // reference to layout fragment (e.g., ImageFragment)

    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new InlineBox(Role)
        {
            TextContent = TextContent,
            Element = Element,
            Style = Style,
            Parent = parent,
            Width = Width,
            Height = Height,
            BaselineOffset = BaselineOffset,
            Fragment = Fragment
        };
    }
}
