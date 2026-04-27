namespace Html2x.LayoutEngine.Models;

/// <summary>
/// Represents parsed floated content while float layout remains unsupported and diagnostic-only.
/// </summary>
public sealed class FloatBox(BoxRole role) : BoxNode(role)
{
    public string FloatDirection { get; init; } = HtmlCssConstants.Defaults.FloatDirection;

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return new FloatBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            FloatDirection = FloatDirection
        };
    }
}
