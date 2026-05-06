using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class InlineBox(BoxRole role) : BoxNode(role)
{
    public string? TextContent { get; init; } // For inline text nodes

    public float Width { get; internal set; }
    public float Height { get; internal set; }

    public SizePt Size
    {
        get => new(Width, Height);
        internal set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    public float BaselineOffset { get; internal set; }

    protected override BoxNode CloneShallowForParent(BoxNode parent) =>
        new InlineBox(Role)
        {
            TextContent = TextContent,
            Element = Element,
            Style = Style,
            Parent = parent,
            SourceIdentity = SourceIdentity,
            Width = Width,
            Height = Height,
            BaselineOffset = BaselineOffset
        };
}