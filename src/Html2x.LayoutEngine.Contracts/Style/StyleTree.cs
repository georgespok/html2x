namespace Html2x.LayoutEngine.Contracts.Style;

internal sealed class StyleTree
{
    public StyleNode? Root { get; set; }
    public PageStyle Page { get; init; } = new();
}