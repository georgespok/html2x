namespace Html2x.Core.Layout;

public class HtmlLayout
{
    public IList<LayoutPage> Pages { get; } = new List<LayoutPage>();
    public LayoutMetadata Metadata { get; } = new();
}