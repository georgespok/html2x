using System.Text;
using html2x.IntegrationTest;
using Html2x.Renderers.Pdf;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Html2x.Test;

public sealed class HtmlConverterTests : IntegrationTestBase, IDisposable
{
    private readonly HtmlConverter _htmlConverter;
    private readonly PdfOptions _options = new() { FontPath = Path.Combine("Fonts", "Inter-Regular.ttf") };
    private readonly ILoggerFactory _loggerFactory;

    public HtmlConverterTests(ITestOutputHelper output)
        : base(output)
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new TestOutputLoggerProvider(output, LogLevel.Trace));
        });

        _htmlConverter = new HtmlConverter(loggerFactory: _loggerFactory);
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
        var pdfBytes = await _htmlConverter.ToPdfAsync(html, _options);

        await SavePdfForInspectionAsync(pdfBytes);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}
