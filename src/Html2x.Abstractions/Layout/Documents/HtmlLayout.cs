namespace Html2x.Abstractions.Layout.Documents;

public class HtmlLayout
{
    public IList<LayoutPage> Pages { get; } = new List<LayoutPage>();
    public LayoutMetadata Metadata { get; } = new();
}