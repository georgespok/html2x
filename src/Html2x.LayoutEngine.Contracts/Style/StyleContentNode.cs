namespace Html2x.LayoutEngine.Contracts.Style;

internal sealed record StyleContentNode(
    StyleContentIdentity Identity,
    StyleContentNodeKind Kind,
    string? Text,
    StyleNode? Element)
{
    public static StyleContentNode LineBreak { get; } = ForLineBreak(StyleContentIdentity.Unspecified);

    public static StyleContentNode ForElement(StyleNode element) =>
        ForElement(StyleContentIdentity.Unspecified, element);

    public static StyleContentNode ForElement(StyleContentIdentity identity, StyleNode element)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(identity);

        return new(identity, StyleContentNodeKind.Element, null, element);
    }

    public static StyleContentNode ForText(string text) => ForText(StyleContentIdentity.Unspecified, text);

    public static StyleContentNode ForText(StyleContentIdentity identity, string text)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return new(identity, StyleContentNodeKind.Text, text, null);
    }

    public static StyleContentNode ForLineBreak(StyleContentIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return new(identity, StyleContentNodeKind.LineBreak, null, null);
    }
}