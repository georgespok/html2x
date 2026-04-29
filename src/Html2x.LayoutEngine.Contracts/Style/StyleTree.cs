namespace Html2x.LayoutEngine.Models;

public sealed class StyleTree
{
    public StyleNode? Root { get; set; }
    public PageStyle Page { get; init; } = new();
}