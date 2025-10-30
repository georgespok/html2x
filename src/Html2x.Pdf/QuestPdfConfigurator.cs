using QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Html2x.Pdf;

public static class QuestPdfConfigurator
{
    public static void Configure(string? fontPath, PdfLicenseType optionsLicenseType)
    {
        Settings.License = MapLicenseType(optionsLicenseType);

        if (string.IsNullOrWhiteSpace(fontPath) || !File.Exists(fontPath))
        {
            Settings.UseEnvironmentFonts = true;
        }
        else
        {
            Settings.UseEnvironmentFonts = false;
            using var fontStream = File.OpenRead(fontPath);
            FontManager.RegisterFont(fontStream);
        }
    }

    private static LicenseType? MapLicenseType(PdfLicenseType licenseType)
    {
        return licenseType switch
        {
            PdfLicenseType.Community => LicenseType.Community,
            PdfLicenseType.Professional => LicenseType.Professional,
            PdfLicenseType.Enterprise => LicenseType.Enterprise,
            _ => throw new ArgumentOutOfRangeException(nameof(licenseType))
        };
    }
}