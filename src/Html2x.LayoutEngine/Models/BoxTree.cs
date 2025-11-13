namespace Html2x.LayoutEngine.Models;

public sealed class BoxTree
{
    public List<BlockBox> Blocks { get; } = [];
    public PageBox Page { get; } = new();
}