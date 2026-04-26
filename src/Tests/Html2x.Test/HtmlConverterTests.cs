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
    public async Task ToPdfAsync_HtmlIsSimple_GenerateValidPdf()
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
    public async Task ToPdfAsync_FontPathIsMissing_ThrowAndEmitDiagnostics()
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
        Assert.Contains(diagnostics.Events, e =>
            e.Name == "LayoutBuild" &&
            e.Type == DiagnosticsEventType.Error &&
            e.StageState == DiagnosticStageState.Failed &&
            e.Description == "PdfOptions.FontPath must be provided before layout can begin.");
        Assert.Contains(diagnostics.Events, e =>
            e.Name == "PdfRender" &&
            e.Type == DiagnosticsEventType.EndStage &&
            e.StageState == DiagnosticStageState.Skipped &&
            e.Description == "Skipped because LayoutBuild failed.");
    }

    [Fact]
    public async Task ToPdfAsync_FontPathIsInvalid_ThrowAndEmitDiagnostics()
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
        Assert.Contains(diagnostics.Events, e =>
            e.Name == "LayoutBuild" &&
            e.Type == DiagnosticsEventType.Error &&
            e.StageState == DiagnosticStageState.Failed &&
            e.Description == exception.Message);
        Assert.Contains(diagnostics.Events, e =>
            e.Name == "PdfRender" &&
            e.Type == DiagnosticsEventType.EndStage &&
            e.StageState == DiagnosticStageState.Skipped &&
            e.Description == "Skipped because LayoutBuild failed.");
    }

    [Fact]
    public async Task ToPdfAsync_DiagnosticsAreEnabled_EmitCanonicalStageLifecycleStates()
    {
        const string html = "<html><body><p>Hello diagnostics</p></body></html>";
        var options = new HtmlConverterOptions
        {
            Pdf = new PdfOptions
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            }
        };

        var result = await _htmlConverter.ToPdfAsync(html, options);

        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics.Events, e =>
            e.Name == "LayoutBuild" &&
            e.Type == DiagnosticsEventType.StartStage &&
            e.StageState == DiagnosticStageState.Started);
        Assert.Contains(result.Diagnostics.Events, e =>
            e.Name == "LayoutBuild" &&
            e.Type == DiagnosticsEventType.EndStage &&
            e.StageState == DiagnosticStageState.Succeeded);
        Assert.Contains(result.Diagnostics.Events, e =>
            e.Name == "PdfRender" &&
            e.Type == DiagnosticsEventType.StartStage &&
            e.StageState == DiagnosticStageState.Started);
        Assert.Contains(result.Diagnostics.Events, e =>
            e.Name == "PdfRender" &&
            e.Type == DiagnosticsEventType.EndStage &&
            e.StageState == DiagnosticStageState.Succeeded);
    }

    [Fact]
    public async Task ToPdfAsync_DiagnosticsEnabled_UsesSingleResolvedFontPath()
    {
        const string html = """
            <html>
              <body>
                <p style="font-family: Inter; font-size: 14pt;">One owner for font resolution.</p>
              </body>
            </html>
            """;

        var options = new HtmlConverterOptions
        {
            Pdf = new PdfOptions
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            }
        };

        var result = await _htmlConverter.ToPdfAsync(html, options);

        Assert.NotNull(result.Diagnostics);

        var fontEvents = result.Diagnostics.Events
            .Where(static x => x.Name == "font/resolve")
            .Select(static x => x.Payload)
            .OfType<FontResolutionPayload>()
            .Where(static x => x.Outcome == "Resolved")
            .ToList();

        var measurement = Assert.Single(fontEvents, static x => x.Consumer == "SkiaTextMeasurer");
        var fragmentStage = Assert.Single(fontEvents, static x => x.Consumer == "InlineFragmentStage");

        Assert.Equal("FontPathSource", measurement.Owner);
        Assert.Equal(measurement.SourceId, fragmentStage.SourceId);
        Assert.Equal(measurement.FilePath, fragmentStage.FilePath);
        Assert.Equal(Path.Combine("Fonts", "Inter-Regular.ttf"), measurement.ConfiguredPath);
        Assert.Equal(Path.Combine("Fonts", "Inter-Regular.ttf"), fragmentStage.ConfiguredPath);
        Assert.DoesNotContain(fontEvents, static x => x.Consumer == "SkiaFontCache");
    }
}

