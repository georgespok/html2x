namespace Html2x.RenderModel.Documents;

/// <summary>
///     Renderer-facing document layout made of immutable page facts.
/// </summary>
public sealed class HtmlLayout
{
    private readonly List<LayoutPage> _pages = [];

    /// <summary>
    ///     Creates an empty layout. Add pages through <see cref="AddPage" />.
    /// </summary>
    public HtmlLayout()
    {
    }

    /// <summary>
    ///     Creates a layout from the supplied pages.
    /// </summary>
    /// <param name="pages">Pages to add in source order.</param>
    public HtmlLayout(IEnumerable<LayoutPage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        foreach (var page in pages)
        {
            AddPage(page);
        }
    }

    /// <summary>
    ///     Gets the pages in source order.
    /// </summary>
    public IReadOnlyList<LayoutPage> Pages => _pages;

    /// <summary>
    ///     Adds a page to the end of the layout.
    /// </summary>
    /// <param name="page">The page to append.</param>
    public void AddPage(LayoutPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        _pages.Add(page);
    }
}