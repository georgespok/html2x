using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Models;

internal sealed class InlineBox(BoxRole role) : BoxNode(role)
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

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return new InlineBox(Role)
        {
            TextContent = TextContent,
            Element = Element,
            Style = Style,
            Parent = parent,
            SourceIdentity = SourceIdentity,
            Width = Width,
            Height = Height,
            BaselineOffset = BaselineOffset,
            Fragment = Fragment
        };
    }
}
