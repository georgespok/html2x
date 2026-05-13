using Html2x.Resources;

namespace Html2x.Renderers.Pdf;

/// <summary>
///     Input settings owned by the PDF renderer.
/// </summary>
internal sealed class PdfRenderSettings
{
    /// <summary>Base directory used to resolve relative image paths during rendering.</summary>
    public string? ResourceBaseDirectory { get; init; }

    /// <summary>Maximum allowed image size in bytes; images over this are marked oversize.</summary>
    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;

    internal IImageResourceReader? ImageResources { get; init; }
}
