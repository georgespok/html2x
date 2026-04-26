namespace Html2x.LayoutEngine.Models;

public sealed class TableSectionBox(DisplayRole role) : DisplayNode(role)
{
    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new TableSectionBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent
        };
    }
}
