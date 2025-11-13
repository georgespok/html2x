namespace Html2x.LayoutEngine.Models;

public sealed class FloatBox : DisplayNode
{
    public string FloatDirection { get; init; } = HtmlCssConstants.Defaults.FloatDirection;
}