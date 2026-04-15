using System.Text.Json;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Shouldly;
using Xunit.Abstractions;

namespace Html2x.Test.Scenarios;

public sealed class DiagnosticsGapScenarioTests(ITestOutputHelper output) : IntegrationTestBase(output)
{
    private static HtmlConverterOptions Options => new()
    {
        Diagnostics = new DiagnosticsOptions
        {
            EnableDiagnostics = true
        },
        Pdf = new PdfOptions
        {
            FontPath = Path.Combine("Fonts", "Inter-Regular.ttf"),
            HtmlDirectory = Path.Combine(GetRepositoryRoot(), "src", "Tests", "Html2x.TestConsole", "html")
        }
    };

    [Fact]
    public async Task ToPdf_DiagnosticsGapSample_ExportsStyleStageSnapshotTableImageAndRawInput()
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
        result.Diagnostics.ShouldNotBeNull();

        var diagnostics = result.Diagnostics!;
        diagnostics.Events.Any(static e =>
            e.Name == "LayoutBuild" &&
            e.StageState == DiagnosticStageState.Started).ShouldBeTrue();
        diagnostics.Events.Any(static e =>
            e.Name == "PdfRender" &&
            e.StageState == DiagnosticStageState.Succeeded).ShouldBeTrue();

        var styleDiagnostics = diagnostics.Events
            .Where(static e => e.Payload is StyleDiagnosticPayload)
            .ToList();
        styleDiagnostics.ShouldContain(static e => e.Name == "style/ignored-declaration");
        styleDiagnostics.Any(static e => e.RawUserInput is not null).ShouldBeTrue();

        var snapshot = diagnostics.Events
            .Where(static e => e.Payload is LayoutSnapshotPayload)
            .Select(static e => ((LayoutSnapshotPayload)e.Payload!).Snapshot)
            .Single();
        var fragments = Flatten(snapshot.Pages.SelectMany(static page => page.Fragments).ToList()).ToList();
        fragments.Any(static fragment => fragment.Color is not null).ShouldBeTrue();
        fragments.Any(static fragment => fragment.Borders is not null && fragment.Borders.HasAny).ShouldBeTrue();
        fragments.Any(static fragment => fragment.DisplayRole is not null).ShouldBeTrue();

        var tablePayload = diagnostics.Events
            .Where(static e => e.Name == "layout/table")
            .Select(static e => e.Payload)
            .OfType<TableLayoutPayload>()
            .Single();
        tablePayload.Outcome.ShouldBe("Supported");
        tablePayload.RowContexts.ShouldNotBeEmpty();
        tablePayload.CellContexts.ShouldNotBeEmpty();
        tablePayload.ColumnContexts.ShouldNotBeEmpty();
        tablePayload.GroupContexts.ShouldNotBeEmpty();

        var imageEvent = diagnostics.Events.Single(static e => e.Name == "image/render");
        imageEvent.Severity.ShouldBe(DiagnosticSeverity.Warning);
        imageEvent.Context.ShouldNotBeNull();
        imageEvent.Context!.RawUserInput.ShouldBe("missing-diagnostics-image.png");
        var imagePayload = imageEvent.Payload.ShouldBeOfType<ImageRenderPayload>();
        imagePayload.Status.ShouldBe(ImageStatus.Missing);
        imagePayload.RenderedSize.Width.ShouldBeGreaterThan(0);

        var json = DiagnosticsSessionSerializer.ToJson(diagnostics);
        using var document = JsonDocument.Parse(json);
        var events = document.RootElement.GetProperty("events").EnumerateArray().ToList();
        events.ShouldContain(static e => e.GetProperty("name").GetString() == "image/render");
        events.ShouldContain(static e => e.GetProperty("rawUserInput").ValueKind == JsonValueKind.String);
        events.Any(HasSerializedRows).ShouldBeTrue();
    }

    private static bool HasSerializedRows(JsonElement e)
    {
        var payload = e.GetProperty("payload");
        return payload.ValueKind == JsonValueKind.Object &&
               payload.TryGetProperty("rowContexts", out var rows) &&
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

    private static IEnumerable<FragmentSnapshot> Flatten(IReadOnlyList<FragmentSnapshot> fragments)
    {
        foreach (var fragment in fragments)
        {
            yield return fragment;

            foreach (var child in Flatten(fragment.Children))
            {
                yield return child;
            }
        }
    }
}
