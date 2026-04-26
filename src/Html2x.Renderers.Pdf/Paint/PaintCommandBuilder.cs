using Html2x.Abstractions.Layout.Documents;

namespace Html2x.Renderers.Pdf.Paint;

/// <summary>
/// Builds ordered internal paint commands from one paginated layout page.
/// </summary>
internal sealed class PaintCommandBuilder
{
    private readonly PaintOrderResolver _resolver;

    public PaintCommandBuilder()
        : this(new PaintOrderResolver())
    {
    }

    internal PaintCommandBuilder(PaintOrderResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public IReadOnlyList<PaintCommand> Build(LayoutPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return _resolver.Resolve(page);
    }
}
