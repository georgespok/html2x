using System.Text;
using html2x.IntegrationTest;
using Html2x.Pdf;
using Xunit.Abstractions;

namespace Html2x.Test;

public class HtmlConverterTests(ITestOutputHelper output) : IntegrationTestBase(output)
{
    private readonly HtmlConverter _htmlConverter = new();
    private readonly PdfOptions _options = new() { FontPath = Path.Combine("Fonts", "Inter-Regular.ttf") };

    [Fact]
    public async Task ConvertSimpleHtmlToPdf_ShouldGenerateValidPdf()
    {
        // Arrange
        const string htmlContent = @"<!DOCTYPE html>
            <html>
              <head>
                <meta charset=""utf-8"" />
                <style>
                  body {
                    font-family: 'Arial';
                    font-size: 12pt;
                    margin: 24pt
                  }
                  
                </style>
              </head>
              <body >
                <h1>Integration Test</h1>
                <p>Hello, Html2x!</p>
                <div>DIV</div>
              </body>
            </html>";

        // Act
        var pdfBytes = await _htmlConverter.ToPdfAsync(htmlContent, _options);

        await SavePdfForInspectionAsync(pdfBytes);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }
}