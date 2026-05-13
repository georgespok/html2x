using System.Text;
using System.Text.Json;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Html2x.Options;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Text;
using Html2x.Text;
using Xunit.Abstractions;

namespace Html2x.Test;

[Trait("Category", "Integration")]
public sealed class HtmlConverterTests(ITestOutputHelper output) : IntegrationTestBase(output)
{
    private const string TwoByOnePngDataUri = $"data:image/png;base64,{TwoByOnePngBase64}";

    private const string TwoByOnePngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8DwHwQBEPgD/U6VwW8AAAAASUVORK5CYII=";

    private readonly HtmlConverter _htmlConverter = new();

    private readonly HtmlConverterOptions _options = new()
    {
        Fonts = new()
        {
            FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
        }
    };

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
            Fonts = new() { FontPath = string.Empty },
            Diagnostics = new() { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _htmlConverter.ToPdfAsync("<html><div>Test</div></html>", options));

        Assert.Contains("FontPath", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(exception.Data.Contains("DiagnosticsReport"));

        var diagnostics = exception.Data["DiagnosticsReport"] as DiagnosticsReport;
        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "Configuration" &&
            e.Name == "font-path/error" &&
            e.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "Configuration" &&
            e.Name == "stage/failed" &&
            e.Severity == DiagnosticSeverity.Error &&
            e.Message == "HtmlConverterOptions.Fonts.FontPath must be provided before layout can begin.");
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because Configuration failed.");
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because Configuration failed.");
    }

    [Fact]
    public async Task ToPdfAsync_FontPathIsInvalid_ThrowAndEmitDiagnostics()
    {
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = Path.Combine(Path.GetTempPath(), "missing-fonts") },
            Diagnostics = new() { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _htmlConverter.ToPdfAsync("<html><div>Test</div></html>", options));

        Assert.Contains("FontPath", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(exception.Data.Contains("DiagnosticsReport"));

        var diagnostics = exception.Data["DiagnosticsReport"] as DiagnosticsReport;
        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "Configuration" &&
            e.Name == "font-path/error" &&
            e.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "Configuration" &&
            e.Name == "stage/failed" &&
            e.Severity == DiagnosticSeverity.Error &&
            e.Message == exception.Message);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because Configuration failed.");
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because Configuration failed.");
    }

    [Theory]
    [MemberData(nameof(NullDependencyFactoryCases))]
    public async Task ToPdfAsync_DependencyFactoryReturnsNull_AttachesConfigurationDiagnostics(
        HtmlConverterDependencies dependencies,
        string expectedMessage)
    {
        var converter = new HtmlConverter(dependencies);
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = null },
            Diagnostics = new() { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            converter.ToPdfAsync("<html><body><p>Dependency failure</p></body></html>", options));

        Assert.Equal(expectedMessage, exception.Message);
        AssertConfigurationFailureDiagnostics(exception, expectedMessage);
    }

    [Fact]
    public async Task ToPdfAsync_DependencyFactoryThrows_AttachesConfigurationDiagnosticsAndPreservesException()
    {
        var expectedException = new DependencyFactoryException("Dependency factory failed.");
        var converter = new HtmlConverter(new()
        {
            TextMeasurerFactory = () => throw expectedException
        });
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = null },
            Diagnostics = new() { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<DependencyFactoryException>(() =>
            converter.ToPdfAsync("<html><body><p>Dependency failure</p></body></html>", options));

        Assert.Same(expectedException, exception);
        AssertConfigurationFailureDiagnostics(exception, expectedException.Message);
    }

    [Fact]
    public async Task ToPdfAsync_DependencyFontSource_AllowsOptionsWithoutFontPath()
    {
        const string html = "<html><body><p>Dependency font source</p></body></html>";
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf");
        var converter = new HtmlConverter(new()
        {
            FontSourceFactory = () => new FixedFontSource(fontPath)
        });
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = null },
            Diagnostics = new() { EnableDiagnostics = true }
        };

        var result = await converter.ToPdfAsync(html, options);

        Assert.NotEmpty(result.PdfBytes);
        Assert.NotNull(result.DiagnosticsReport);
        Assert.Contains(result.DiagnosticsReport.Records, static record =>
            record.Name == "font/resolve" &&
            StringField(record, "owner") == nameof(FixedFontSource));
    }

    [Fact]
    public async Task ToPdfAsync_DependencyTextMeasurer_AllowsOptionsWithoutFontPath()
    {
        const string html = "<html><body><p>Dependency text measurer</p></body></html>";
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf");
        var textMeasurers = new List<CountingDependencyTextMeasurer>();
        var converter = new HtmlConverter(new()
        {
            TextMeasurerFactory = () =>
            {
                var textMeasurer = new CountingDependencyTextMeasurer(fontPath);
                textMeasurers.Add(textMeasurer);
                return textMeasurer;
            }
        });
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = null }
        };

        var result = await converter.ToPdfAsync(html, options);

        Assert.NotEmpty(result.PdfBytes);
        Assert.Single(textMeasurers);
        Assert.True(textMeasurers.Sum(static textMeasurer => textMeasurer.MeasureCount) > 0);
    }

    [Fact]
    public async Task ToPdfAsync_DependencyTextMeasurerFactory_DisposesConversionScopedMeasurer()
    {
        const string html = "<html><body><p>Dependency text measurer disposal</p></body></html>";
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf");
        var textMeasurers = new List<DisposableDependencyTextMeasurer>();
        var converter = new HtmlConverter(new()
        {
            TextMeasurerFactory = () =>
            {
                var textMeasurer = new DisposableDependencyTextMeasurer(fontPath);
                textMeasurers.Add(textMeasurer);
                return textMeasurer;
            }
        });
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = null }
        };

        await converter.ToPdfAsync(html, options);

        var created = Assert.Single(textMeasurers);
        Assert.True(created.MeasureCount > 0);
        Assert.True(created.Disposed);
    }

    [Fact]
    public async Task ToPdfAsync_DependencyTextMeasurerReturnsNull_ThrowsInvalidOperationException()
    {
        var converter = new HtmlConverter(new()
        {
            TextMeasurerFactory = () => new NullTextMeasurer()
        });
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = null }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            converter.ToPdfAsync("<html><body><p>Invalid measurement</p></body></html>", options));

        Assert.Contains("ITextMeasurer.Measure returned null.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToPdfAsync_DependencyTextMeasurerReturnsInvalidMeasurement_ThrowsOutOfRange()
    {
        var converter = new HtmlConverter(new()
        {
            TextMeasurerFactory = () => new InvalidTextMeasurer()
        });
        var options = new HtmlConverterOptions
        {
            Fonts = new() { FontPath = null }
        };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            converter.ToPdfAsync("<html><body><p>Invalid measurement</p></body></html>", options));

        Assert.Equal("Width", exception.ParamName);
    }

    [Fact]
    public void Constructor_DependenciesAreNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new HtmlConverter(null!));

        Assert.Equal("dependencies", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(InvalidPageSizeCases))]
    public async Task ToPdfAsync_InvalidPageSize_ThrowsArgumentOutOfRangeException(
        float width,
        float height)
    {
        var options = new HtmlConverterOptions
        {
            Page = new() { Size = new SizePt(width, height) },
            Diagnostics = new() { EnableDiagnostics = true }
        };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _htmlConverter.ToPdfAsync("<html><body><p>Invalid page size</p></body></html>", options));

        Assert.Equal("HtmlConverterOptions.Page.Size", exception.ParamName);
        Assert.Contains("HtmlConverterOptions.Page.Size", exception.Message, StringComparison.Ordinal);
        Assert.False(exception.Data.Contains(nameof(HtmlToPdfResult.DiagnosticsReport)));
    }

    [Fact]
    public async Task ToPdfAsync_InvalidResourceBaseDirectory_ThrowsWithoutDiagnostics()
    {
        var options = new HtmlConverterOptions
        {
            Resources = new()
            {
                BaseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            },
            Diagnostics = new()
            {
                EnableDiagnostics = true
            }
        };

        var exception = await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _htmlConverter.ToPdfAsync("<html><body><p>Invalid resource base directory</p></body></html>", options));

        Assert.Contains("HtmlConverterOptions.Resources.BaseDirectory", exception.Message, StringComparison.Ordinal);
        Assert.False(exception.Data.Contains(nameof(HtmlToPdfResult.DiagnosticsReport)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToPdfAsync_InvalidMaxImageSizeBytes_ThrowsWithoutDiagnostics(long maxImageSizeBytes)
    {
        var options = new HtmlConverterOptions
        {
            Resources = new()
            {
                MaxImageSizeBytes = maxImageSizeBytes
            },
            Diagnostics = new()
            {
                EnableDiagnostics = true
            }
        };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _htmlConverter.ToPdfAsync("<html><body><p>Invalid image limit</p></body></html>", options));

        Assert.Equal("MaxImageSizeBytes", exception.ParamName);
        Assert.False(exception.Data.Contains(nameof(HtmlToPdfResult.DiagnosticsReport)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToPdfAsync_InvalidMaxRawHtmlLength_ThrowsWithoutDiagnostics(int maxRawHtmlLength)
    {
        var options = new HtmlConverterOptions
        {
            Diagnostics = new()
            {
                EnableDiagnostics = true,
                MaxRawHtmlLength = maxRawHtmlLength
            }
        };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _htmlConverter.ToPdfAsync("<html><body><p>Invalid raw HTML limit</p></body></html>", options));

        Assert.Equal("MaxRawHtmlLength", exception.ParamName);
        Assert.False(exception.Data.Contains(nameof(HtmlToPdfResult.DiagnosticsReport)));
    }

    [Fact]
    public async Task ToPdfAsync_NullDiagnosticsOptions_ThrowsArgumentNullException()
    {
        var options = new HtmlConverterOptions
        {
            Diagnostics = null!
        };

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _htmlConverter.ToPdfAsync("<html><body><p>Invalid diagnostics options</p></body></html>", options));

        Assert.Equal("Diagnostics", exception.ParamName);
        Assert.False(exception.Data.Contains(nameof(HtmlToPdfResult.DiagnosticsReport)));
    }

    [Fact]
    public async Task ToPdfAsync_DiagnosticsAreEnabled_EmitCanonicalStageLifecycleStates()
    {
        const string html = "<html><body><p>Hello diagnostics</p></body></html>";
        var options = new HtmlConverterOptions
        {
            Fonts = new()
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new()
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
                [1, 2]);
            const string html = """
                                <html>
                                  <body>
                                    <img src="oversize.png" width="16" height="16" />
                                  </body>
                                </html>
                                """;
            var options = new HtmlConverterOptions
            {
                Fonts = new()
                {
                    FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
                },
                Resources = new()
                {
                    BaseDirectory = tempDirectory.FullName,
                    MaxImageSizeBytes = 1
                },
                Diagnostics = new()
                {
                    EnableDiagnostics = true
                }
            };

            var result = await _htmlConverter.ToPdfAsync(html, options);

            Assert.NotNull(result.DiagnosticsReport);
            var imageRecord = Assert.Single(
                result.DiagnosticsReport.Records,
                static record => record.Name == "image/render");
            Assert.Equal("Oversized", StringField(imageRecord, "status"));
        }
        finally
        {
            tempDirectory.Delete(true);
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
            Assert.Equal(30d, NumberField(imageRecord, "renderedWidth"), 1);
            Assert.Equal(15d, NumberField(imageRecord, "renderedHeight"), 1);
        }
        finally
        {
            tempDirectory.Delete(true);
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
        Assert.Equal(30d, NumberField(imageRecord, "renderedWidth"), 1);
        Assert.Equal(15d, NumberField(imageRecord, "renderedHeight"), 1);
    }

    [Fact]
    public async Task ToPdfAsync_ImageResources_ReportDetailedRecoverableStatuses()
    {
        var rootDirectory = Directory.CreateTempSubdirectory();
        var baseDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "base"));

        try
        {
            await File.WriteAllBytesAsync(Path.Combine(rootDirectory.FullName, "outside.png"), TwoByOnePngBytes());
            await File.WriteAllBytesAsync(Path.Combine(baseDirectory.FullName, "oversize.png"), [1, 2]);

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
                Fonts = new()
                {
                    FontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf")
                },
                Resources = new()
                {
                    BaseDirectory = baseDirectory.FullName,
                    MaxImageSizeBytes = 1
                },
                Diagnostics = new()
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
            Assert.Equal("Oversized", StringField(imageRecords[2], "status"));
            Assert.Equal("InvalidDataUri", StringField(imageRecords[3], "status"));
            Assert.Equal("DecodeFailed", StringField(imageRecords[4], "status"));
        }
        finally
        {
            rootDirectory.Delete(true);
        }
    }

    [Fact]
    public async Task ToPdfAsync_DiagnosticsAreEnabled_ExposesSerializableDiagnosticsReport()
    {
        const string html = "<html><body><p>Hello diagnostics report</p></body></html>";
        var options = new HtmlConverterOptions
        {
            Fonts = new()
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new()
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
            record.Severity == DiagnosticSeverity.Info);
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
        var htmlLengthField = Assert.IsType<DiagnosticNumberValue>(
            layoutStart.Fields["htmlLength"]);
        Assert.Equal(html.Length, htmlLengthField.Value);
        Assert.False(layoutStart.Fields.ContainsKey("html"));

        var layoutSucceeded = Assert.Single(report.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/succeeded");
        var layoutSnapshot = Assert.IsType<DiagnosticObject>(
            layoutSucceeded.Fields["snapshot"]);
        Assert.Equal(
            new DiagnosticNumberValue(1),
            layoutSnapshot["pageCount"]);

        var geometrySnapshot = Assert.Single(report.Records, static record =>
            record.Name == "layout/geometry-snapshot");
        var geometryFields = Assert.IsType<DiagnosticObject>(
            geometrySnapshot.Fields["snapshot"]);
        Assert.True(geometryFields.ContainsKey("fragments"));
        Assert.True(geometryFields.ContainsKey("boxes"));
        Assert.True(geometryFields.ContainsKey("pagination"));

        var json = DiagnosticsReportSerializer.ToJson(report);
        using var document = JsonDocument.Parse(json);
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
            Fonts = new()
            {
                FontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new()
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

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _htmlConverter.ToPdfAsync(html, CreateDiagnosticsOptions(), cancellation.Token));

        var diagnostics = Assert.IsType<DiagnosticsReport>(exception.Data["DiagnosticsReport"]);
        Assert.Contains(diagnostics.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/started");
        Assert.Contains(diagnostics.Records, static record =>
            record.Stage == "LayoutBuild" &&
            record.Name == "stage/canceled");
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
            Fonts = new()
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
            },
            Diagnostics = new()
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

    private static void AssertConfigurationFailureDiagnostics(Exception exception, string expectedMessage)
    {
        Assert.True(exception.Data.Contains(nameof(HtmlToPdfResult.DiagnosticsReport)));
        var diagnostics = Assert.IsType<DiagnosticsReport>(
            exception.Data[nameof(HtmlToPdfResult.DiagnosticsReport)]);

        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "Configuration" &&
            e.Name == "stage/failed" &&
            e.Severity == DiagnosticSeverity.Error &&
            e.Message == expectedMessage);
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because Configuration failed.");
        Assert.Contains(diagnostics.Records, e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/skipped" &&
            e.Message == "Skipped because Configuration failed.");
    }

    private static DiagnosticRecord SingleImageRecord(HtmlToPdfResult result)
    {
        Assert.NotNull(result.DiagnosticsReport);
        return Assert.Single(result.DiagnosticsReport.Records, static record => record.Name == "image/render");
    }

    private static HtmlConverterOptions CreateDiagnosticsOptions(string? baseDirectory = null) =>
        new()
        {
            Fonts = new()
            {
                FontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter-Regular.ttf")
            },
            Resources = new()
            {
                BaseDirectory = baseDirectory
            },
            Diagnostics = new()
            {
                EnableDiagnostics = true
            }
        };

    private static byte[] TwoByOnePngBytes() =>
        Convert.FromBase64String(TwoByOnePngBase64);

    public static IEnumerable<object[]> InvalidPageSizeCases()
    {
        yield return [0f, PaperSizes.Letter.Height];
        yield return [-1f, PaperSizes.Letter.Height];
        yield return [float.NaN, PaperSizes.Letter.Height];
        yield return [float.PositiveInfinity, PaperSizes.Letter.Height];
        yield return [float.NegativeInfinity, PaperSizes.Letter.Height];
        yield return [PaperSizes.Letter.Width, 0f];
        yield return [PaperSizes.Letter.Width, -1f];
        yield return [PaperSizes.Letter.Width, float.NaN];
        yield return [PaperSizes.Letter.Width, float.PositiveInfinity];
        yield return [PaperSizes.Letter.Width, float.NegativeInfinity];
    }

    public static IEnumerable<object[]> NullDependencyFactoryCases()
    {
        yield return
        [
            new HtmlConverterDependencies
            {
                FontSourceFactory = () => null!
            },
            "HtmlConverterDependencies.FontSourceFactory returned null."
        ];
        yield return
        [
            new HtmlConverterDependencies
            {
                TextMeasurerFactory = () => null!
            },
            "HtmlConverterDependencies.TextMeasurerFactory returned null."
        ];
    }

    private sealed class DependencyFactoryException(string message) : Exception(message);

    private sealed class FixedFontSource(string fontPath) : IFontSource
    {
        public ResolvedFont Resolve(FontKey requested, string consumer) =>
            new(
                requested.Family,
                requested.Weight,
                requested.Style,
                fontPath,
                fontPath,
                0,
                fontPath);
    }

    private sealed class CountingDependencyTextMeasurer(string fontPath) : ITextMeasurer
    {
        public int MeasureCount { get; private set; }

        public TextMeasurement Measure(FontKey font, float sizePt, string text)
        {
            MeasureCount++;
            return new(
                string.IsNullOrEmpty(text) ? 0f : text.Length * 5f,
                8f,
                3f,
                new(
                    font.Family,
                    font.Weight,
                    font.Style,
                    fontPath,
                    fontPath,
                0,
                fontPath));
        }
    }

    private sealed class DisposableDependencyTextMeasurer(string fontPath) : ITextMeasurer, IDisposable
    {
        public int MeasureCount { get; private set; }

        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;

        public TextMeasurement Measure(FontKey font, float sizePt, string text)
        {
            MeasureCount++;
            return new(
                string.IsNullOrEmpty(text) ? 0f : text.Length * 5f,
                8f,
                3f,
                new(
                    font.Family,
                    font.Weight,
                    font.Style,
                    fontPath,
                    fontPath,
                    0,
                    fontPath));
        }
    }

    private sealed class NullTextMeasurer : ITextMeasurer
    {
        public TextMeasurement Measure(FontKey font, float sizePt, string text) => null!;
    }

    private sealed class InvalidTextMeasurer : ITextMeasurer
    {
        public TextMeasurement Measure(FontKey font, float sizePt, string text) =>
            new(
                float.NaN,
                8f,
                3f,
                new(
                    font.Family,
                    font.Weight,
                    font.Style,
                    "test://font"));
    }
}
