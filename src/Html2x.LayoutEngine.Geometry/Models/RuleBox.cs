namespace Html2x.LayoutEngine.Models;

/// <summary>
/// Represents an HTML horizontal rule element that lays out as a block and projects to a rule fragment.
/// </summary>
internal sealed class RuleBox(BoxRole role) : BlockBox(role)
{
    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new RuleBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity
        });
    }
}
