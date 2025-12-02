using Html2x.Abstractions.Images;

namespace Html2x.LayoutEngine.Fragment;

public readonly record struct FragmentBuildContext(
    IImageProvider ImageProvider,
    string HtmlDirectory,
    long MaxImageSizeBytes);
