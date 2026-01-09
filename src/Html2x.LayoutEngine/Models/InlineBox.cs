using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Models;

public sealed class InlineBox : DisplayNode
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
}
