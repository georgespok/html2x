using System.Text;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Xunit.Abstractions;

namespace Html2x.Test;

public sealed class HtmlConverterTests : IntegrationTestBase
{
    private readonly HtmlConverter _htmlConverter;
    private readonly HtmlConverterOptions _options = new()
    {
        Fonts = new FontOptions
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
            Fonts = new FontOptions { FontPath = string.Empty },
            Diagnostics = new DiagnosticsOptions { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _htmlConverter.ToPdfAsync("<html><div>Test</div></html>", options));

        Assert.Contains("FontPath", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(exception.Data.Contains("DiagnosticsReport"));

        var diagnostics = exception.Data["DiagnosticsReport"] as DiagnosticsReport;
        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "Configuration" &&
            e.Name == "font-path/error" &&
            e.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/failed" &&
            e.Severity == DiagnosticSeverity.Error &&
            e.Message == "HtmlConverterOptions.Fonts.FontPath must be provided before layout can begin.");
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because LayoutBuild failed.");
    }

    [Fact]
    public async Task ToPdfAsync_FontPathIsInvalid_ThrowAndEmitDiagnostics()
    {
        var options = new HtmlConverterOptions
        {
            Fonts = new FontOptions { FontPath = Path.Combine(Path.GetTempPath(), "missing-fonts") },
            Diagnostics = new DiagnosticsOptions { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _htmlConverter.ToPdfAsync("<html><div>Test</div></html>", options));

        Assert.Contains("FontPath", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(exception.Data.Contains("DiagnosticsReport"));

        var diagnostics = exception.Data["DiagnosticsReport"] as DiagnosticsReport;
        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "Configuration" &&
            e.Name == "font-path/error" &&
            e.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/failed" &&
            e.Severity == DiagnosticSeverity.Error &&
            e.Message == exception.Message);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because LayoutBuild failed.");
    }

    [Fact]
    public async Task ToPdfAsync_DiagnosticsAreEnabled_EmitCanonicalStageLifecycleStates()
    {
        const string html = "<html><body><p>Hello diagnostics</p></body></html>";
        var options = new HtmlConverterOptions
        {
            Fonts = new FontOptions
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            }
        };

        var result = await _htmlConverter.ToPdfAsync(html, options);

        Assert.NotNull(result.DiagnosticsReport);
        Assert.Contains(result.DiagnosticsReport.Records, e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/started");
        Assert.Contains(result.DiagnosticsReport.Records, e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/succeeded");
        Assert.Contains(result.DiagnosticsReport.Records, e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/started");
        Assert.Contains(result.DiagnosticsReport.Records, e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/succeeded");
    }

    [Fact]
    public async Task ToPdfAsync_ResourceOptionsApplySingleImageSizePolicy()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            await File.WriteAllBytesAsync(
                Path.Combine(tempDirectory.FullName, "oversize.png"),
                new byte[] { 1, 2 });
            const string html = """
                <html>
                  <body>
                    <img src="oversize.png" width="16" height="16" />
                  </body>
                </html>
                """;
            var options = new HtmlConverterOptions
            {
                Fonts = new FontOptions
                {
                    FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
                },
                Resources = new ResourceOptions
                {
                    BaseDirectory = tempDirectory.FullName,
                    MaxImageSizeBytes = 1
                },
                Diagnostics = new DiagnosticsOptions
                {
                    EnableDiagnostics = true
                }
            };

            var result = await _htmlConverter.ToPdfAsync(html, options);

            Assert.NotNull(result.DiagnosticsReport);
            var imageRecord = Assert.Single(
                result.DiagnosticsReport.Records,
                static record => record.Name == "image/render");
            Assert.Equal("Oversize", StringField(imageRecord, "status"));
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ToPdfAsync_DiagnosticsAreEnabled_ExposesSerializableDiagnosticsReport()
    {
        const string html = "<html><body><p>Hello diagnostics report</p></body></html>";
        var options = new HtmlConverterOptions
        {
            Fonts = new FontOptions
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            }
        };

        var result = await _htmlConverter.ToPdfAsync(html, options);

        Assert.NotNull(result.DiagnosticsReport);
        var report = result.DiagnosticsReport;
        Assert.Contains(report.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/started" &&
            record.Severity == global::Html2x.Diagnostics.Contracts.DiagnosticSeverity.Info);
        Assert.Contains(report.Records, static record =>
            record.Stage == "stage/dom" &&
            record.Name == "stage/succeeded");
        Assert.Contains(report.Records, static record =>
            record.Stage == "stage/pagination" &&
            record.Name == "stage/succeeded");
        Assert.Contains(report.Records, static record =>
            record.Stage == "PdfRender" &&
            record.Name == "stage/succeeded");

        var layoutStart = Assert.Single(report.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/started");
        var htmlField = Assert.IsType<global::Html2x.Diagnostics.Contracts.DiagnosticStringValue>(
            layoutStart.Fields["html"]);
        Assert.Equal(html, htmlField.Value);

        var layoutSucceeded = Assert.Single(report.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/succeeded");
        var layoutSnapshot = Assert.IsType<global::Html2x.Diagnostics.Contracts.DiagnosticObject>(
            layoutSucceeded.Fields["snapshot"]);
        Assert.Equal(
            new global::Html2x.Diagnostics.Contracts.DiagnosticNumberValue(1),
            layoutSnapshot["pageCount"]);

        var geometrySnapshot = Assert.Single(report.Records, static record =>
            record.Name == "layout/geometry-snapshot");
        var geometryFields = Assert.IsType<global::Html2x.Diagnostics.Contracts.DiagnosticObject>(
            geometrySnapshot.Fields["snapshot"]);
        Assert.True(geometryFields.ContainsKey("fragments"));
        Assert.True(geometryFields.ContainsKey("boxes"));
        Assert.True(geometryFields.ContainsKey("pagination"));

        var json = global::Html2x.Diagnostics.DiagnosticsReportSerializer.ToJson(report);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var records = document.RootElement.GetProperty("records").EnumerateArray().ToArray();
        Assert.Contains(records, static record =>
            record.GetProperty("stage").GetString() == "PdfRender" &&
            record.GetProperty("name").GetString() == "stage/succeeded");
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
            Fonts = new FontOptions
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            }
        };

        var result = await _htmlConverter.ToPdfAsync(html, options);

        Assert.NotNull(result.DiagnosticsReport);

        var resolvedFontEvents = result.DiagnosticsReport.Records
            .Where(static x => x.Name == "font/resolve")
            .Where(static x => x.Fields["outcome"] is DiagnosticStringValue { Value: "Resolved" })
            .ToList();

        var fontRecord = Assert.Single(
            resolvedFontEvents,
            static x => StringField(x, "consumer") == "SkiaTextMeasurer");

        Assert.Equal("stage/font", fontRecord.Stage);
        Assert.Equal("Resolved", StringField(fontRecord, "outcome"));
        Assert.Equal("FontPathSource", StringField(fontRecord, "owner"));
        Assert.Equal(Path.Combine("Fonts", "Inter-Regular.ttf"), StringField(fontRecord, "configuredPath"));
        Assert.DoesNotContain(resolvedFontEvents, static x => StringField(x, "consumer") == "FragmentBuilder");
        Assert.DoesNotContain(resolvedFontEvents, static x => StringField(x, "consumer") == "SkiaFontCache");
    }

    private static string StringField(DiagnosticRecord record, string fieldName) =>
        Assert.IsType<DiagnosticStringValue>(record.Fields[fieldName]).Value;
}
