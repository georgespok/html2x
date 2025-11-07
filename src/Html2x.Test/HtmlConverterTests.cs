using System.Text;
using html2x.IntegrationTest;
using Html2x.Pdf;
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
              <head>
                <meta charset='utf-8' />
                <style>
                  body {
                    font-family: 'Arial';
                    font-size: 12pt;
                    margin: 24pt
                  }
                  
                </style>
              </head>
              <body>

                <h1>H1</h1>
                <h2>H2</h2>
                <h3>H3</h3>
                <h4>H4</h4>
                <h5>H5</h5>
                <h6>H6</h6>

                <p>Paragraph</p>

                <div>
                    <span>Span inside Div</span> 
                    <p>
                        Paragraph inside Div
                        <div>
                            Nested Div inside Paragraph
                            <span>Nested Span inside nested Div</span>
                        </div>
                    </p>
                </div>

                <ul>
                    <li>Unordered item 1</li>
                    <li>Unordered item 2</li>
                </ul>

                <ol>
                    <li>Ordered item 1</li>
                    <li>Ordered item 2</li>
                </ol>

                <div style='border-width: 1px; border-style: dashed;'>
                    Text box
                </div>

                <div style='border-width: 1px; border-style: dashed; margin: 30px'>
                    Text with border around
                </div>

                <div style='border-width: 1px; border-style: dashed;'>
                    Text box
                </div>

              </body>
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
