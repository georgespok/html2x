using Html2x.Abstractions.Images;

namespace Html2x.LayoutEngine.Box;

public sealed class BoxTreeBuildContext(
    IImageProvider imageProvider,
    string htmlDirectory,
    long maxImageSizeBytes)
{
    public IImageProvider ImageProvider { get; } = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));

    public string HtmlDirectory { get; } = htmlDirectory ?? throw new ArgumentNullException(nameof(htmlDirectory));

    public long MaxImageSizeBytes { get; } = maxImageSizeBytes;
}
