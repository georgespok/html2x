namespace Html2x.LayoutEngine.Models;

public sealed class InlineBox : DisplayNode
{
    public string? TextContent { get; init; } // For inline text nodes

    public float Width { get; set; }
    public float Height { get; set; }
    public float BaselineOffset { get; set; }

    public object? Fragment { get; set; } // reference to layout fragment (e.g., ImageFragment)
}
