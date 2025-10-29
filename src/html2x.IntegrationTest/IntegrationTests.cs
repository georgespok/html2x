using System.Text;
using Html2x.Layout;
using Html2x.Pdf;
using Xunit.Abstractions;

namespace html2x.IntegrationTest;

public class IntegrationTests(ITestOutputHelper output) : IntegrationTestBase(output)
{
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
                    font-family: ""Arial"";
                    font-size: 12pt;
                    margin: 24pt;
                  }
                  h1 {
                    font-size: 18pt;
                    margin-bottom: 8pt;
                  }
                </style>
              </head>
              <body>
                <h1>Integration Test</h1>
                <p>Hello, Html2x!</p>
              </body>
            </html>";

        var options = new PdfOptions { ForntPath = Path.Combine("Fonts", "Inter-Regular.ttf") };
        var layoutBuilder = new LayoutBuilder();
        var pdfRenderer = new PdfRenderer();

        // Act
        var layout = await layoutBuilder.BuildAsync(htmlContent);
        var pdfBytes = await pdfRenderer.RenderAsync(layout, options);
        await SavePdfForInspectionAsync(pdfBytes);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }
}