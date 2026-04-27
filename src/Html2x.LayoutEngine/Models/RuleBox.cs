namespace Html2x.LayoutEngine.Models;

public sealed class RuleBox(BoxRole role) : BlockBox(role)
{
    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new RuleBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
        });
    }
}
