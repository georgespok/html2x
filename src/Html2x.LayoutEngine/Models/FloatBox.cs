namespace Html2x.LayoutEngine.Models;

/// <summary>
/// Represents parsed floated content while float layout remains unsupported and diagnostic-only.
/// </summary>
public sealed class FloatBox(DisplayRole role) : DisplayNode(role)
{
    public string FloatDirection { get; init; } = HtmlCssConstants.Defaults.FloatDirection;

    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
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
