namespace Html2x.LayoutEngine.Geometry.Models;

/// <summary>
///     Represents parsed floated content while float layout remains unsupported and diagnostic-only.
/// </summary>
internal sealed class FloatBox(BoxRole role) : BoxNode(role)
{
    public string FloatDirection { get; init; } = HtmlCssConstants.Defaults.FloatDirection;

    protected override BoxNode CloneShallowForParent(BoxNode parent) =>
        new FloatBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            SourceIdentity = SourceIdentity,
            FloatDirection = FloatDirection
        };
}