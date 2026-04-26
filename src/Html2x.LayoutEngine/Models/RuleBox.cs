namespace Html2x.LayoutEngine.Models;

public sealed class RuleBox(DisplayRole role) : BlockBox(role)
{
    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new RuleBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Margin = Margin,
            Padding = Padding,
            TextAlign = TextAlign,
            MarkerOffset = MarkerOffset,
            UsedGeometry = UsedGeometry,
            IsAnonymous = IsAnonymous,
            IsInlineBlockContext = IsInlineBlockContext
        };
    }
}
