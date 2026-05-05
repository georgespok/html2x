namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Compatibility adapter for callers that have not moved to <see cref="TableGridLayout"/>.
/// </summary>
internal sealed class TableLayoutEngine
{
    private readonly TableGridLayout _tableGridLayout;

    public TableLayoutEngine()
        : this(new InlineLayoutEngine(), new ImageLayoutResolver())
    {
    }

    internal TableLayoutEngine(InlineLayoutEngine inlineEngine, IImageLayoutResolver? imageResolver = null)
    {
        _tableGridLayout = new TableGridLayout(inlineEngine, imageResolver);
    }

    public TableLayoutResult Layout(TableBox table, float availableWidth)
    {
        return _tableGridLayout.Layout(table, availableWidth);
    }
}
