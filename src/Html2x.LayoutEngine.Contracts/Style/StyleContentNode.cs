namespace Html2x.LayoutEngine.Models;

public sealed record StyleContentNode(
    StyleContentIdentity Identity,
    StyleContentNodeKind Kind,
    string? Text,
    StyleNode? Element)
{
    public static StyleContentNode ForElement(StyleNode element)
    {
        return ForElement(StyleContentIdentity.Unspecified, element);
    }

    public static StyleContentNode ForElement(StyleContentIdentity identity, StyleNode element)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(identity);

        return new StyleContentNode(identity, StyleContentNodeKind.Element, null, element);
    }

    public static StyleContentNode ForText(string text)
    {
        return ForText(StyleContentIdentity.Unspecified, text);
    }

    public static StyleContentNode ForText(StyleContentIdentity identity, string text)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return new StyleContentNode(identity, StyleContentNodeKind.Text, text, null);
    }

    public static StyleContentNode ForLineBreak(StyleContentIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return new StyleContentNode(identity, StyleContentNodeKind.LineBreak, null, null);
    }

    public static StyleContentNode LineBreak { get; } = ForLineBreak(StyleContentIdentity.Unspecified);
}
