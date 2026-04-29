using Html2x.Abstractions.Images;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
/// Carries runtime inputs required to compute layout geometry for one document.
/// </summary>
public sealed class LayoutGeometryRequest
{
    public SizePt PageSize { get; init; } = PaperSizes.Letter;

    public IImageProvider? ImageProvider { get; init; }

    public string HtmlDirectory { get; init; } = Directory.GetCurrentDirectory();

    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;

    public static LayoutGeometryRequest Default { get; } = new();
}
