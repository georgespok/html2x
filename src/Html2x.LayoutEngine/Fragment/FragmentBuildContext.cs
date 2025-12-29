using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuildContext
{
    public FragmentBuildContext(
        IImageProvider imageProvider,
        string htmlDirectory,
        long maxImageSizeBytes,
        ITextMeasurer textMeasurer,
        IFontSource fontSource)
    {
        ImageProvider = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));
        HtmlDirectory = htmlDirectory ?? throw new ArgumentNullException(nameof(htmlDirectory));
        MaxImageSizeBytes = maxImageSizeBytes;
        TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        FontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
    }

    public IImageProvider ImageProvider { get; }

    public string HtmlDirectory { get; }

    public long MaxImageSizeBytes { get; }

    public ITextMeasurer TextMeasurer { get; }

    public IFontSource FontSource { get; }
}
