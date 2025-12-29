using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;

namespace Html2x;

/// <summary>
/// Provides layout dependencies from the composition layer.
/// </summary>
public sealed class LayoutServices(ITextMeasurer textMeasurer, IFontSource fontSource)
{
    public ITextMeasurer TextMeasurer { get; } = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    public IFontSource FontSource { get; } = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
}
