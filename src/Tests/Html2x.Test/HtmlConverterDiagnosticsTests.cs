using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Shouldly;

namespace Html2x.Test;

public sealed class HtmlConverterDiagnosticsTests
{
    private static HtmlConverterOptions DiagnosticsOptions => new()
    {
        Diagnostics = new DiagnosticsOptions
        {
            EnableDiagnostics = true
        },
        Pdf = new PdfOptions
        {
            FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
        }
    };

    [Fact]
    public async Task ToPdfAsync_ReportTemplateWithInvalidStyles_EmitsStyleDiagnostics()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body>
                <section id="invoice" style="width: 10rem; padding: 1px 2px 3px 4px 5px;">
                  <h1>Invoice</h1>
                  <p>Total due</p>
                </section>
              </body>
            </html>
            """;

        var converter = new HtmlConverter();
        var result = await converter.ToPdfAsync(html, DiagnosticsOptions);

        result.PdfBytes.ShouldNotBeEmpty();
        result.DiagnosticsReport.ShouldNotBeNull();

        var diagnostics = result.DiagnosticsReport!;
        var styleDiagnostics = diagnostics.Records
            .Where(static x => x.Stage == "stage/style" && x.Name.StartsWith("style/", StringComparison.Ordinal))
            .ToList();

        styleDiagnostics.ShouldContain(static x => x.Name == "style/unsupported-declaration");
        styleDiagnostics.ShouldContain(static x => x.Name == "style/ignored-declaration");

        var unsupportedWidth = styleDiagnostics
            .Single(static x => x.Fields["propertyName"] is DiagnosticStringValue { Value: "width" });

        StringField(unsupportedWidth, "rawValue").ShouldBe("10rem");
        StringField(unsupportedWidth, "decision").ShouldBe("Unsupported");
        StringField(unsupportedWidth, "reason").ShouldContain("Unsupported unit");
        unsupportedWidth.Context.ShouldNotBeNull();
        unsupportedWidth.Context.ElementIdentity.ShouldBe("section#invoice");
        unsupportedWidth.Context.StyleDeclaration.ShouldBe("width: 10rem");

        var ignoredPadding = styleDiagnostics
            .Single(static x => x.Fields["propertyName"] is DiagnosticStringValue { Value: "padding" });

        StringField(ignoredPadding, "rawValue").ShouldBe("1px 2px 3px 4px 5px");
        StringField(ignoredPadding, "decision").ShouldBe("Ignored");
        StringField(ignoredPadding, "reason").ShouldContain("expected 1 to 4");
        ignoredPadding.Context.ShouldNotBeNull();
        ignoredPadding.Context.ElementIdentity.ShouldBe("section#invoice");
        ignoredPadding.Context.StyleDeclaration.ShouldBe("padding: 1px 2px 3px 4px 5px");
    }

    [Fact]
    public async Task ToPdfAsync_EmptyFontDirectory_EmitsFontResolutionFailureDiagnostics()
    {
        const string html = "<html><body><p>Missing font evidence</p></body></html>";
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            var options = new HtmlConverterOptions
            {
                Diagnostics = new DiagnosticsOptions
                {
                    EnableDiagnostics = true
                },
                Pdf = new PdfOptions
                {
                    FontPath = tempDirectory.FullName
                }
            };

            var converter = new HtmlConverter();
            var exception = await Should.ThrowAsync<InvalidOperationException>(() => converter.ToPdfAsync(html, options));

            var diagnostics = exception.Data["DiagnosticsReport"].ShouldBeOfType<DiagnosticsReport>();
            var payload = diagnostics.Records
                .Where(static x => x.Name == "font/resolve")
                .Single(static x =>
                    x.Fields["consumer"] is DiagnosticStringValue { Value: "SkiaTextMeasurer" } &&
                    x.Fields["outcome"] is DiagnosticStringValue { Value: "Failed" });

            StringField(payload, "owner").ShouldBe("FontPathSource");
            StringField(payload, "configuredPath").ShouldBe(tempDirectory.FullName);
            StringField(payload, "reason").ShouldContain("not found in directory");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ToPdfAsync_InvalidFontFile_EmitsFontResolutionFailureDiagnostics()
    {
        const string html = "<html><body><p>Invalid font evidence</p></body></html>";
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            var invalidFontPath = Path.Combine(tempDirectory.FullName, "invalid.ttf");
            await File.WriteAllTextAsync(invalidFontPath, "not-a-real-font");

            var options = new HtmlConverterOptions
            {
                Diagnostics = new DiagnosticsOptions
                {
                    EnableDiagnostics = true
                },
                Pdf = new PdfOptions
                {
                    FontPath = invalidFontPath
                }
            };

            var converter = new HtmlConverter();
            var exception = await Should.ThrowAsync<InvalidOperationException>(() => converter.ToPdfAsync(html, options));

            var diagnostics = exception.Data["DiagnosticsReport"].ShouldBeOfType<DiagnosticsReport>();
            var payload = diagnostics.Records
                .Where(static x => x.Name == "font/resolve")
                .Single(static x =>
                    x.Fields["consumer"] is DiagnosticStringValue { Value: "SkiaTextMeasurer" } &&
                    x.Fields["outcome"] is DiagnosticStringValue { Value: "Failed" });

            StringField(payload, "owner").ShouldBe("FontPathSource");
            StringField(payload, "configuredPath").ShouldBe(invalidFontPath);
            payload.Fields["filePath"].ShouldBeNull();
            StringField(payload, "reason").ShouldContain("Failed to load font file");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    private static string StringField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticStringValue>().Value;
}
