using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Shouldly;
using Xunit.Abstractions;

namespace Html2x.Test.Scenarios;

public sealed class PaginationRenderedToPdfTests(ITestOutputHelper output) : IntegrationTestBase(output)
{
    private HtmlConverterOptions DefaultOptions => new()
    {
        Diagnostics = new DiagnosticsOptions
        {
            EnableDiagnostics = true
        },
        Pdf = new PdfOptions
        {
            FontPath = Path.Combine("Fonts", "Inter-Regular.ttf"),
            EnableDebugging = true
        }
    };

    [Fact]
    public async Task ToPdf_WithSameLongInput_ProducesDeterministicPaginationSnapshot()
    {
        var blocks = Enumerable.Range(1, 14)
            .Select(static i => $"<div style='height: 300px;'>Block {i}</div>");
        var html = $"<html><body style='margin: 0;'>{string.Join(string.Empty, blocks)}</body></html>";

        var converter = new HtmlConverter();
        var first = await converter.ToPdfAsync(html, DefaultOptions);
        var second = await converter.ToPdfAsync(html, DefaultOptions);

        var firstSnapshot = GetLayoutSnapshot(first);
        var secondSnapshot = GetLayoutSnapshot(second);

        firstSnapshot.PageCount.ShouldBe(secondSnapshot.PageCount);
        firstSnapshot.PageCount.ShouldBeGreaterThan(1);

        var firstAssignments = ExtractTextAssignments(firstSnapshot);
        var secondAssignments = ExtractTextAssignments(secondSnapshot);
        firstAssignments.ShouldBe(secondAssignments);
    }

    [Fact]
    public async Task ToPdf_WithOversizedAndEmptyContent_EmitsPaginationDiagnostics()
    {
        var oversizedHtml = """
            <html>
              <body style='margin: 0;'>
                <div style='height: 100px;'>intro</div>
                <div style='height: 1400px;'>oversized</div>
              </body>
            </html>
            """;
        const string emptyHtml = "   ";
        var converter = new HtmlConverter();

        var oversized = await converter.ToPdfAsync(oversizedHtml, DefaultOptions);
        var empty = await converter.ToPdfAsync(emptyHtml, DefaultOptions);

        var oversizedEventNames = oversized.Diagnostics!.Events
            .Where(static e => e.Type == DiagnosticsEventType.Trace)
            .Select(static e => e.Name)
            .ToList();
        oversizedEventNames.ShouldContain("layout/pagination/oversized-block");

        var oversizedPayload = oversized.Diagnostics!.Events
            .Where(static e => e.Name == "layout/pagination/oversized-block")
            .Select(static e => (PaginationTracePayload)e.Payload!)
            .First();
        oversizedPayload.BlockHeight.GetValueOrDefault()
            .ShouldBeGreaterThan(oversizedPayload.PageContentHeight.GetValueOrDefault());

        var emptyEventNames = empty.Diagnostics!.Events
            .Where(static e => e.Type == DiagnosticsEventType.Trace)
            .Select(static e => e.Name)
            .ToList();
        emptyEventNames.ShouldContain("layout/pagination/empty-document");

        var emptySnapshot = GetLayoutSnapshot(empty);
        emptySnapshot.PageCount.ShouldBe(1);
        emptySnapshot.Pages[0].Fragments.ShouldBeEmpty();
    }

    private static LayoutSnapshot GetLayoutSnapshot(Html2PdfResult result)
    {
        var layoutEvent = result.Diagnostics!.Events.FirstOrDefault(
            static x => x is { Type: DiagnosticsEventType.EndStage, Payload: not null, Name: "LayoutBuild" });
        layoutEvent.ShouldNotBeNull();

        return ((LayoutSnapshotPayload)layoutEvent.Payload!).Snapshot;
    }

    private static IReadOnlyList<(int PageNumber, string Text, float Y)> ExtractTextAssignments(LayoutSnapshot snapshot)
    {
        return snapshot.Pages
            .OrderBy(static page => page.PageNumber)
            .SelectMany(static page =>
                EnumerateText(page.Fragments)
                    .Where(static item => item.Text.StartsWith("Block ", StringComparison.Ordinal))
                    .Select(item => (page.PageNumber, item.Text, item.Y)))
            .ToList();
    }

    private static IEnumerable<(string Text, float Y)> EnumerateText(IReadOnlyList<FragmentSnapshot> fragments)
    {
        foreach (var fragment in fragments)
        {
            if (!string.IsNullOrWhiteSpace(fragment.Text))
            {
                yield return (fragment.Text.Trim(), fragment.Y);
            }

            foreach (var child in EnumerateText(fragment.Children))
            {
                yield return child;
            }
        }
    }
}
