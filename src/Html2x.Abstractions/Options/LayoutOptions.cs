using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Options;

public sealed class LayoutOptions
{
    public SizePt PageSize { get; init; } = PaperSizes.Letter;

    /// <summary>Base directory used to resolve relative image paths.</summary>
    public string HtmlDirectory { get; init; } = Directory.GetCurrentDirectory();

    /// <summary>Maximum allowed image size in bytes; images over this are marked oversize.</summary>
    public long MaxImageSizeBytes { get; init; } = (long)(10 * 1024 * 1024);

    /// <summary>Use the embedded default user agent stylesheet when no override is provided.</summary>
    public bool UseDefaultUserAgentStyleSheet { get; init; } = true;

    /// <summary>Overrides the embedded user agent stylesheet when set to a non-empty value.</summary>
    public string? UserAgentStyleSheet { get; init; }
}
