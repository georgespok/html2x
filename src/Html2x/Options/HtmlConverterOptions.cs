using Html2x.RenderModel;

namespace Html2x;

/// <summary>
/// Public conversion request options for HTML to PDF conversion.
/// </summary>
public sealed class HtmlConverterOptions
{
    public PageOptions Page { get; init; } = new();

    public ResourceOptions Resources { get; init; } = new();

    public CssOptions Css { get; init; } = new();

    public FontOptions Fonts { get; init; } = new();

    public DiagnosticsOptions Diagnostics { get; init; } = new();
}

/// <summary>
/// Page-level conversion options.
/// </summary>
public sealed class PageOptions
{
    public SizePt Size { get; init; } = PaperSizes.Letter;
}

/// <summary>
/// Resource loading options shared by layout and rendering.
/// </summary>
public sealed class ResourceOptions
{
    /// <summary>Base directory used to resolve relative resource paths.</summary>
    public string BaseDirectory { get; init; } = Directory.GetCurrentDirectory();

    /// <summary>Maximum allowed image size in bytes; images over this are marked oversize.</summary>
    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;
}

/// <summary>
/// CSS processing options.
/// </summary>
public sealed class CssOptions
{
    /// <summary>Use the embedded default user agent stylesheet when no override is provided.</summary>
    public bool UseDefaultUserAgentStyleSheet { get; init; } = true;

    /// <summary>Overrides the embedded user agent stylesheet when set to a non-empty value.</summary>
    public string? UserAgentStyleSheet { get; init; }
}

/// <summary>
/// Font resolution options.
/// </summary>
public sealed class FontOptions
{
    public string? FontPath { get; init; }
}

/// <summary>
/// Diagnostics options.
/// </summary>
public sealed class DiagnosticsOptions
{
    public bool EnableDiagnostics { get; init; }
}
