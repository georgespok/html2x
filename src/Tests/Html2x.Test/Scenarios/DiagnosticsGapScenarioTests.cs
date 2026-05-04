using System.Text.Json;
using Html2x.Diagnostics.Contracts;
using Html2x.Diagnostics;
using Shouldly;
using Xunit.Abstractions;

namespace Html2x.Test.Scenarios;

[Trait("Category", "Integration")]
public sealed class DiagnosticsGapScenarioTests(ITestOutputHelper output) : IntegrationTestBase(output)
{
    private static HtmlConverterOptions Options => new()
    {
        Diagnostics = new DiagnosticsOptions
        {
            EnableDiagnostics = true
        },
        Fonts = new FontOptions
        {
            FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
        },
        Resources = new ResourceOptions
        {
            BaseDirectory = Path.Combine(GetRepositoryRoot(), "src", "Tests", "Html2x.TestConsole", "html")
        }
    };

    [Fact]
    public async Task ToPdf_DiagnosticsGapSample_ExportsDiagnosticPayloads()
    {
        var htmlPath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Tests",
            "Html2x.TestConsole",
            "html",
            "diagnostics-gap.html");
        var html = await File.ReadAllTextAsync(htmlPath);
        var converter = new HtmlConverter();

        var result = await converter.ToPdfAsync(html, Options);

        result.PdfBytes.ShouldNotBeEmpty();
        result.DiagnosticsReport.ShouldNotBeNull();

        var diagnostics = result.DiagnosticsReport!;
        diagnostics.Records.Any(static e =>
            e.Stage == "LayoutBuild" &&
            e.Name == "stage/started").ShouldBeTrue();
        diagnostics.Records.Any(static e =>
            e.Stage == "PdfRender" &&
            e.Name == "stage/succeeded").ShouldBeTrue();

        var styleDiagnostics = diagnostics.Records
            .Where(static e => e.Stage == "stage/style" && e.Name.StartsWith("style/", StringComparison.Ordinal))
            .ToList();
        styleDiagnostics.ShouldContain(static e => e.Name == "style/ignored-declaration");
        styleDiagnostics.Any(static e => e.Context?.RawUserInput is not null).ShouldBeTrue();

        var snapshot = diagnostics.Records
            .Single(static e => e is { Stage: "LayoutBuild", Name: "stage/succeeded" })
            .Fields["snapshot"]
            .ShouldBeOfType<DiagnosticObject>();
        var fragments = Flatten(ArrayField(snapshot, "pages")
                .Select(static page => page.ShouldBeOfType<DiagnosticObject>())
                .SelectMany(static page => ArrayField(page, "fragments")))
            .ToList();
        fragments.Any(static fragment => fragment["color"] is DiagnosticStringValue).ShouldBeTrue();
        fragments.Any(static fragment => fragment["borders"] is DiagnosticObject { Count: > 0 }).ShouldBeTrue();
        fragments.Any(static fragment => fragment["displayRole"] is DiagnosticStringValue).ShouldBeTrue();

        var tablePayload = diagnostics.Records
            .Where(static e => e.Name == "layout/table")
            .Single();
        StringField(tablePayload, "outcome").ShouldBe("Supported");
        ArrayField(tablePayload, "rows").ShouldNotBeEmpty();
        ArrayField(tablePayload, "cells").ShouldNotBeEmpty();
        ArrayField(tablePayload, "columns").ShouldNotBeEmpty();
        ArrayField(tablePayload, "groups").ShouldNotBeEmpty();

        var imageEvent = diagnostics.Records.Single(static e => e.Name == "image/render");
        imageEvent.Severity.ShouldBe(DiagnosticSeverity.Warning);
        imageEvent.Context.ShouldNotBeNull();
        imageEvent.Context!.RawUserInput.ShouldBe("missing-diagnostics-image.png");
        StringField(imageEvent, "status").ShouldBe("Missing");
        NumberField(imageEvent, "renderedWidth").ShouldBeGreaterThan(0);

        var json = DiagnosticsReportSerializer.ToJson(diagnostics);
        using var document = JsonDocument.Parse(json);
        var records = document.RootElement.GetProperty("records").EnumerateArray().ToList();
        records.ShouldContain(static e => e.GetProperty("name").GetString() == "image/render");
        records.Any(static e =>
            e.TryGetProperty("context", out var context) &&
            context.ValueKind == JsonValueKind.Object &&
            context.TryGetProperty("rawUserInput", out var rawUserInput) &&
            rawUserInput.ValueKind == JsonValueKind.String).ShouldBeTrue();
        records.Any(HasSerializedRows).ShouldBeTrue();
    }

    private static bool HasSerializedRows(JsonElement e)
    {
        var fields = e.GetProperty("fields");
        return fields.ValueKind == JsonValueKind.Object &&
               fields.TryGetProperty("rows", out var rows) &&
               rows.GetArrayLength() > 0;
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Html2x.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static IEnumerable<DiagnosticObject> Flatten(IEnumerable<DiagnosticValue?> fragments)
    {
        foreach (var fragment in fragments)
        {
            var fragmentObject = fragment.ShouldBeOfType<DiagnosticObject>();
            yield return fragmentObject;

            foreach (var child in Flatten(ArrayField(fragmentObject, "children")))
            {
                yield return child;
            }
        }
    }

    private static DiagnosticArray ArrayField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticArray>();

    private static DiagnosticArray ArrayField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticArray>();

    private static string StringField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticStringValue>().Value;

    private static double NumberField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;
}
