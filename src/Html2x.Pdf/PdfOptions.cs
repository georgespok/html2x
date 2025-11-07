using Html2x.Core;

namespace Html2x.Pdf;

public enum PdfLicenseType
{
    Community,
    Professional,
    Enterprise
}

public class PdfOptions
{
    public string? FontPath { get; set; }
    public PdfLicenseType LicenseType { get; set; } = PdfLicenseType.Community;
    public Dimensions PageSize { get; set; } = PaperSizes.Letter;
    public bool EnableDebugging { get; set; } = false;
}