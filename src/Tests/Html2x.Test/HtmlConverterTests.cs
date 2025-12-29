using System.Text;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Xunit.Abstractions;

namespace Html2x.Test;

public sealed class HtmlConverterTests : IntegrationTestBase
{
    private readonly HtmlConverter _htmlConverter;
    private readonly HtmlConverterOptions _options = new()
    {
        Pdf = new PdfOptions
        {
            FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
        }
    };

    public HtmlConverterTests(ITestOutputHelper output)
        : base(output)
    {
        _htmlConverter = new HtmlConverter();
    }

    [Fact]
    public async Task ConvertSimpleHtmlToPdf_ShouldGenerateValidPdf()
    {
        // Arrange
        const string html = @"<!DOCTYPE html>
            <html>
                <div style=""border-width: 1px; border-style: dashed;"">
                     TopBox
                 </div>
                 <div style=""border-width: 1px; border-style: dashed; "">
                     Padding 30px
                 </div>
                 <div style=""border-width: 1px; border-style: dashed;"">
                     BottomBox
                 </div>
            </html>";

        // Act
        var result = await _htmlConverter.ToPdfAsync(html, _options);

        await SavePdfForInspectionAsync(result.PdfBytes);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.PdfBytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(result.PdfBytes, 0, 4));
    }

    [Fact]
    public async Task MissingFontPath_ShouldThrowAndEmitDiagnostics()
    {
        var options = new HtmlConverterOptions
        {
            Pdf = new PdfOptions { FontPath = string.Empty },
            Diagnostics = new DiagnosticsOptions { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _htmlConverter.ToPdfAsync("<html><div>Test</div></html>", options));

        Assert.Contains("FontPath", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(exception.Data.Contains("Diagnostics"));

        var diagnostics = exception.Data["Diagnostics"] as DiagnosticsSession;
        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticsEventType.Error && e.Name == "FontPath");
    }

    [Fact]
    public async Task InvalidFontPath_ShouldThrowAndEmitDiagnostics()
    {
        var options = new HtmlConverterOptions
        {
            Pdf = new PdfOptions { FontPath = Path.Combine(Path.GetTempPath(), "missing-fonts") },
            Diagnostics = new DiagnosticsOptions { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _htmlConverter.ToPdfAsync("<html><div>Test</div></html>", options));

        Assert.Contains("FontPath", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(exception.Data.Contains("Diagnostics"));

        var diagnostics = exception.Data["Diagnostics"] as DiagnosticsSession;
        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticsEventType.Error && e.Name == "FontPath");
    }

}

