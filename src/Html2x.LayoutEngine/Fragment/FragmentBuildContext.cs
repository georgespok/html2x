using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Formatting;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuildContext
{
    public FragmentBuildContext(
        IImageProvider imageProvider,
        string htmlDirectory,
        long maxImageSizeBytes,
        ITextMeasurer textMeasurer,
        IFontSource fontSource)
        : this(
            imageProvider,
            htmlDirectory,
            maxImageSizeBytes,
            textMeasurer,
            fontSource,
            new BlockFormattingContext())
    {
    }

    internal FragmentBuildContext(
        IImageProvider imageProvider,
        string htmlDirectory,
        long maxImageSizeBytes,
        ITextMeasurer textMeasurer,
        IFontSource fontSource,
        IBlockFormattingContext blockFormattingContext)
    {
        ImageProvider = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));
        HtmlDirectory = htmlDirectory ?? throw new ArgumentNullException(nameof(htmlDirectory));
        MaxImageSizeBytes = maxImageSizeBytes;
        TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        FontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
        BlockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
    }

    public IImageProvider ImageProvider { get; }

    public string HtmlDirectory { get; }

    public long MaxImageSizeBytes { get; }

    public ITextMeasurer TextMeasurer { get; }

    public IFontSource FontSource { get; }

    internal IBlockFormattingContext BlockFormattingContext { get; }
}
