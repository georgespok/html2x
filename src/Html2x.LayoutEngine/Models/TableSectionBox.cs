namespace Html2x.LayoutEngine.Models;

public sealed class TableSectionBox(BoxRole role) : BoxNode(role)
{
    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return new TableSectionBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent
        };
    }
}
