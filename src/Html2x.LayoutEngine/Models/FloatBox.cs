namespace Html2x.LayoutEngine.Models;

public sealed class FloatBox(DisplayRole role) : DisplayNode(role)
{
    public string FloatDirection { get; init; } = HtmlCssConstants.Defaults.FloatDirection;
}
