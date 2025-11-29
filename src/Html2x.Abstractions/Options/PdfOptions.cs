using Html2x.Abstractions.Measurements.Units;
using System.IO;

namespace Html2x.Abstractions.Options;

public class PdfOptions
{
    public string? FontPath { get; set; }
    public PdfLicenseType LicenseType { get; set; } = PdfLicenseType.Community;
    public PageSize PageSize { get; set; } = PaperSizes.Letter;
    public bool EnableDebugging { get; set; } = false;
    /// <summary>
    /// Base directory used to resolve relative image paths; defaults to the current working directory.
    /// </summary>
    public string HtmlDirectory { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Maximum allowed image size in megabytes before rejection/placeholder.
    /// </summary>
    public double MaxImageSizeMb { get; set; } = 10;

    /// <summary>
    /// Maximum allowed image size in bytes (derived from <see cref="MaxImageSizeMb" />).
    /// </summary>
    public long MaxImageSizeBytes => (long)(MaxImageSizeMb * 1024 * 1024);
}




