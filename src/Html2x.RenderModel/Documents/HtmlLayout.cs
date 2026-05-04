namespace Html2x.RenderModel;

public class HtmlLayout
{
    private readonly List<LayoutPage> _pages = [];

    public IReadOnlyList<LayoutPage> Pages => _pages;

    public LayoutMetadata Metadata { get; } = new();

    public HtmlLayout()
    {
    }

    public HtmlLayout(IEnumerable<LayoutPage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        foreach (var page in pages)
        {
            AddPage(page);
        }
    }

    public void AddPage(LayoutPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        _pages.Add(page);
    }
}
