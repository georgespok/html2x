using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine;

/// <summary>
/// Provides layout dependencies from the composition layer.
/// </summary>
public sealed class LayoutServices(ITextMeasurer textMeasurer, IFontSource fontSource, IImageProvider imageProvider)
{
    public IImageProvider ImageProvider { get; } = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));

    public ITextMeasurer TextMeasurer { get; } = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    public IFontSource FontSource { get; } = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
}
