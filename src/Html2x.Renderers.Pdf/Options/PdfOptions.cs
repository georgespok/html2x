using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Renderers.Pdf.Options;

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
    public PageSize PageSize { get; set; } = PaperSizes.Letter;
    public bool EnableDebugging { get; set; } = false;
}




