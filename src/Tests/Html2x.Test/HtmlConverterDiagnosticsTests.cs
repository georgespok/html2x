using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
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
        result.Diagnostics.ShouldNotBeNull();

        var diagnostics = result.Diagnostics!;
        var styleDiagnostics = diagnostics.Events
            .Where(static x => x.Payload is StyleDiagnosticPayload)
            .ToList();

        styleDiagnostics.ShouldContain(static x => x.Name == "style/unsupported-declaration");
        styleDiagnostics.ShouldContain(static x => x.Name == "style/ignored-declaration");

        var unsupportedWidth = styleDiagnostics
            .Select(static x => x.Payload)
            .OfType<StyleDiagnosticPayload>()
            .Single(static x => x.PropertyName == "width");

        unsupportedWidth.RawValue.ShouldBe("10rem");
        unsupportedWidth.Decision.ShouldBe("Unsupported");
        unsupportedWidth.Reason.ShouldContain("Unsupported unit");
        unsupportedWidth.Context.ShouldNotBeNull();
        unsupportedWidth.Context.ElementIdentity.ShouldBe("section#invoice");
        unsupportedWidth.Context.StyleDeclaration.ShouldBe("width: 10rem");

        var ignoredPadding = styleDiagnostics
            .Select(static x => x.Payload)
            .OfType<StyleDiagnosticPayload>()
            .Single(static x => x.PropertyName == "padding");

        ignoredPadding.RawValue.ShouldBe("1px 2px 3px 4px 5px");
        ignoredPadding.Decision.ShouldBe("Ignored");
        ignoredPadding.Reason.ShouldContain("expected 1 to 4");
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

            var diagnostics = exception.Data["Diagnostics"].ShouldBeOfType<DiagnosticsSession>();
            var payload = diagnostics.Events
                .Where(static x => x.Name == "font/resolve")
                .Select(static x => x.Payload)
                .OfType<FontResolutionPayload>()
                .Single(static x => x.Consumer == "SkiaTextMeasurer" && x.Outcome == "Failed");

            payload.Owner.ShouldBe("FontPathSource");
            payload.ConfiguredPath.ShouldBe(tempDirectory.FullName);
            payload.Reason.ShouldNotBeNull();
            payload.Reason.ShouldContain("not found in directory");
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

            var diagnostics = exception.Data["Diagnostics"].ShouldBeOfType<DiagnosticsSession>();
            var payload = diagnostics.Events
                .Where(static x => x.Name == "font/resolve")
                .Select(static x => x.Payload)
                .OfType<FontResolutionPayload>()
                .Single(static x => x.Consumer == "SkiaTextMeasurer" && x.Outcome == "Failed");

            payload.Owner.ShouldBe("FontPathSource");
            payload.ConfiguredPath.ShouldBe(invalidFontPath);
            payload.FilePath.ShouldBeNull();
            payload.Reason.ShouldNotBeNull();
            payload.Reason.ShouldContain("Failed to load font file");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
