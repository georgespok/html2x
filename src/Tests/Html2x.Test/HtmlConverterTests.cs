using System.Text;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Xunit.Abstractions;

namespace Html2x.Test;

[Trait("Category", "Integration")]
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
    public async Task ToPdfAsync_FileImageWithWidthOnly_UsesDecodedIntrinsicRatio()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            await File.WriteAllBytesAsync(
                Path.Combine(tempDirectory.FullName, "ratio.png"),
                TwoByOnePngBytes());
            const string html = """
                <html>
                  <body>
                    <img src="ratio.png" width="40" />
                  </body>
                </html>
                """;

            var result = await _htmlConverter.ToPdfAsync(
                html,
                CreateDiagnosticsOptions(tempDirectory.FullName));

            var imageRecord = SingleImageRecord(result);
            Assert.Equal("Ok", StringField(imageRecord, "status"));
            Assert.Equal(30d, NumberField(imageRecord, "renderedWidth"), precision: 1);
            Assert.Equal(15d, NumberField(imageRecord, "renderedHeight"), precision: 1);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ToPdfAsync_DataUriImageWithHeightOnly_UsesDecodedIntrinsicRatio()
    {
        var html = $"""
            <html>
              <body>
                <img src="{TwoByOnePngDataUri}" height="20" />
              </body>
            </html>
            """;

        var result = await _htmlConverter.ToPdfAsync(html, CreateDiagnosticsOptions());

        var imageRecord = SingleImageRecord(result);
        Assert.Equal("Ok", StringField(imageRecord, "status"));
        Assert.Equal(30d, NumberField(imageRecord, "renderedWidth"), precision: 1);
        Assert.Equal(15d, NumberField(imageRecord, "renderedHeight"), precision: 1);
    }

    [Fact]
    public async Task ToPdfAsync_ImageResources_ReportDetailedRecoverableStatuses()
    {
        var rootDirectory = Directory.CreateTempSubdirectory();
        var baseDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "base"));

        try
        {
            await File.WriteAllBytesAsync(Path.Combine(rootDirectory.FullName, "outside.png"), TwoByOnePngBytes());
            await File.WriteAllBytesAsync(Path.Combine(baseDirectory.FullName, "oversize.png"), new byte[] { 1, 2 });

            const string html = """
                <html>
                  <body>
                    <img src="missing.png" width="16" height="16" />
                    <img src="../outside.png" width="16" height="16" />
                    <img src="oversize.png" width="16" height="16" />
                    <img src="data:image/png;base64,not-base64" width="16" height="16" />
                    <img src="data:image/png;base64,eA==" width="16" height="16" />
                  </body>
                </html>
                """;
            var options = new HtmlConverterOptions
            {
                Fonts = new FontOptions
                {
                    FontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf")
                },
                Resources = new ResourceOptions
                {
                    BaseDirectory = baseDirectory.FullName,
                    MaxImageSizeBytes = 1
                },
                Diagnostics = new DiagnosticsOptions
                {
                    EnableDiagnostics = true
                }
            };

            var result = await _htmlConverter.ToPdfAsync(html, options);

            var imageRecords = result.DiagnosticsReport!.Records
                .Where(static record => record.Name == "image/render")
                .ToList();

            Assert.Equal(5, imageRecords.Count);
            Assert.Equal("Missing", StringField(imageRecords[0], "status"));
            Assert.Equal("OutOfScope", StringField(imageRecords[1], "status"));
            Assert.Equal("Oversize", StringField(imageRecords[2], "status"));
            Assert.Equal("InvalidDataUri", StringField(imageRecords[3], "status"));
            Assert.Equal("DecodeFailed", StringField(imageRecords[4], "status"));
        }
        finally
        {
            rootDirectory.Delete(recursive: true);
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
        var htmlLengthField = Assert.IsType<global::Html2x.Diagnostics.Contracts.DiagnosticNumberValue>(
            layoutStart.Fields["htmlLength"]);
        Assert.Equal(html.Length, htmlLengthField.Value);
        Assert.False(layoutStart.Fields.ContainsKey("html"));

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
    public async Task ToPdfAsync_RawHtmlDiagnosticsOptIn_CapsPayload()
    {
        const string html = "<html><body><p>Hello raw diagnostics payload</p></body></html>";
        var options = new HtmlConverterOptions
        {
            Fonts = new FontOptions
            {
                FontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true,
                IncludeRawHtml = true,
                MaxRawHtmlLength = 18
            }
        };

        var result = await _htmlConverter.ToPdfAsync(html, options);

        var layoutStart = Assert.Single(result.DiagnosticsReport!.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/started");

        Assert.Equal(html[..18], StringField(layoutStart, "html"));
        Assert.True(BoolField(layoutStart, "htmlTruncated"));
    }

    [Fact]
    public async Task ToPdfAsync_CancellationRequested_EmitsCancellationLifecycle()
    {
        const string html = "<html><body><p>cancel me</p></body></html>";
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => _htmlConverter.ToPdfAsync(html, CreateDiagnosticsOptions(), cancellation.Token));

        var diagnostics = Assert.IsType<DiagnosticsReport>(exception.Data["DiagnosticsReport"]);
        Assert.Contains(diagnostics.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/started");
        Assert.Contains(diagnostics.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/cancelled");
        Assert.Contains(diagnostics.Records, static record =>
            record.Stage == "PdfRender" &&
            record.Name == "stage/skipped" &&
            record.Message == "Skipped because LayoutBuild was canceled.");
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

    private static double NumberField(DiagnosticRecord record, string fieldName) =>
        Assert.IsType<DiagnosticNumberValue>(record.Fields[fieldName]).Value;

    private static bool BoolField(DiagnosticRecord record, string fieldName) =>
        Assert.IsType<DiagnosticBooleanValue>(record.Fields[fieldName]).Value;

    private static DiagnosticRecord SingleImageRecord(Html2PdfResult result)
    {
        Assert.NotNull(result.DiagnosticsReport);
        return Assert.Single(result.DiagnosticsReport.Records, static record => record.Name == "image/render");
    }

    private static HtmlConverterOptions CreateDiagnosticsOptions(string? baseDirectory = null) =>
        new()
        {
            Fonts = new FontOptions
            {
                FontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf")
            },
            Resources = new ResourceOptions
            {
                BaseDirectory = baseDirectory
            },
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            }
        };

    private static byte[] TwoByOnePngBytes() =>
        Convert.FromBase64String(TwoByOnePngBase64);

    private const string TwoByOnePngDataUri = $"data:image/png;base64,{TwoByOnePngBase64}";

    private const string TwoByOnePngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8DwHwQBEPgD/U6VwW8AAAAASUVORK5CYII=";
}
