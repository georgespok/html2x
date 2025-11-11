namespace Html2x.Abstractions.Layout;

public class HtmlLayout
{
    public IList<LayoutPage> Pages { get; } = new List<LayoutPage>();
    public LayoutMetadata Metadata { get; } = new();
}