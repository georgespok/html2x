using Html2x.Abstractions.Measurements.Units;
using System.IO;

namespace Html2x.Abstractions.Options;

public sealed class LayoutOptions
{
    public PageSize PageSize { get; init; } = PaperSizes.Letter;

    /// <summary>Base directory used to resolve relative image paths.</summary>
    public string HtmlDirectory { get; init; } = Directory.GetCurrentDirectory();

    /// <summary>Maximum allowed image size in bytes; images over this are marked oversize.</summary>
    public long MaxImageSizeBytes { get; init; } = (long)(10 * 1024 * 1024);
}
