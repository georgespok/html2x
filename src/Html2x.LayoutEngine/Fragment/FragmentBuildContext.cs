using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuildContext(
    IImageProvider imageProvider,
    string htmlDirectory,
    long maxImageSizeBytes,
    ITextMeasurer textMeasurer,
    IFontSource fontSource)
{
    public IImageProvider ImageProvider { get; } = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));

    public string HtmlDirectory { get; } = htmlDirectory ?? throw new ArgumentNullException(nameof(htmlDirectory));

    public long MaxImageSizeBytes { get; } = maxImageSizeBytes;

    public ITextMeasurer TextMeasurer { get; } = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    public IFontSource FontSource { get; } = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
}
