using Html2x.Diagnostics.Contracts;
using Html2x.Abstractions.Options;
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
    public async Task ToPdf_SameLongInput_ProducesDeterministicPaginationSnapshot()
    {
        var blocks = Enumerable.Range(1, 14)
            .Select(static i => $"<div style='height: 300px;'>Block {i}</div>");
        var html = $"<html><body style='margin: 0;'>{string.Join(string.Empty, blocks)}</body></html>";

        var converter = new HtmlConverter();
        var first = await converter.ToPdfAsync(html, DefaultOptions);
        var second = await converter.ToPdfAsync(html, DefaultOptions);

        var firstSnapshot = GetLayoutSnapshot(first);
        var secondSnapshot = GetLayoutSnapshot(second);

        NumberField(firstSnapshot, "pageCount").ShouldBe(NumberField(secondSnapshot, "pageCount"));
        NumberField(firstSnapshot, "pageCount").ShouldBeGreaterThan(1);

        var firstAssignments = ExtractTextAssignments(firstSnapshot);
        var secondAssignments = ExtractTextAssignments(secondSnapshot);
        firstAssignments.ShouldBe(secondAssignments);
    }

    [Fact]
    public async Task ToPdf_OversizedAndEmptyContent_EmitsPaginationDiagnostics()
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

        var oversizedRecords = oversized.DiagnosticsReport!.Records;
        var oversizedEventNames = oversizedRecords
            .Where(static e => e.Stage == "stage/pagination")
            .Select(static e => e.Name)
            .ToList();
        oversizedEventNames.ShouldContain("layout/pagination/oversized-block");

        var paginationEvents = oversizedRecords
            .Where(static e => e.Stage == "stage/pagination" && e.Name.StartsWith("layout/pagination/", StringComparison.Ordinal))
            .ToList();
        paginationEvents.Select(static e => e.Name).ShouldBe(paginationEvents
            .Select(static e => e.Fields["eventName"].ShouldBeOfType<DiagnosticStringValue>().Value));
        paginationEvents.All(static e => e.Context is not null).ShouldBeTrue();
        paginationEvents.All(static e => Enum.IsDefined(e.Severity)).ShouldBeTrue();

        var oversizedEvent = oversizedRecords
            .Where(static e => e.Name == "layout/pagination/oversized-block")
            .First();
        oversizedEvent.Severity.ShouldBe(DiagnosticSeverity.Warning);
        oversizedEvent.Context!.ElementIdentity.ShouldStartWith("fragment#");

        oversizedEvent.Context!.StructuralPath.ShouldStartWith("page[");
        NumberField(oversizedEvent, "blockHeight")
            .ShouldBeGreaterThan(NumberField(oversizedEvent, "pageContentHeight"));

        var emptyEventNames = empty.DiagnosticsReport!.Records
            .Where(static e => e.Stage == "stage/pagination")
            .Select(static e => e.Name)
            .ToList();
        emptyEventNames.ShouldContain("layout/pagination/empty-document");

        var emptySnapshot = GetLayoutSnapshot(empty);
        NumberField(emptySnapshot, "pageCount").ShouldBe(1);
        var firstPage = ArrayField(emptySnapshot, "pages")[0].ShouldBeOfType<DiagnosticObject>();
        ArrayField(firstPage, "fragments").ShouldBeEmpty();
    }

    private static DiagnosticObject GetLayoutSnapshot(Html2PdfResult result)
    {
        var layoutEvent = result.DiagnosticsReport!.Records.FirstOrDefault(
            static x => x is { Stage: "LayoutBuild", Name: "stage/succeeded" });
        layoutEvent.ShouldNotBeNull();

        return layoutEvent.Fields["snapshot"].ShouldBeOfType<DiagnosticObject>();
    }

    private static IReadOnlyList<(int PageNumber, string Text, double Y)> ExtractTextAssignments(DiagnosticObject snapshot)
    {
        return ArrayField(snapshot, "pages")
            .Select(static page => page.ShouldBeOfType<DiagnosticObject>())
            .OrderBy(static page => NumberField(page, "pageNumber"))
            .SelectMany(static page =>
                EnumerateText(ArrayField(page, "fragments"))
                    .Where(static item => item.Text.StartsWith("Block ", StringComparison.Ordinal))
                    .Select(item => ((int)NumberField(page, "pageNumber"), item.Text, item.Y)))
            .ToList();
    }

    private static IEnumerable<(string Text, double Y)> EnumerateText(DiagnosticArray fragments)
    {
        foreach (var fragment in fragments)
        {
            var fragmentObject = fragment.ShouldBeOfType<DiagnosticObject>();
            var text = StringFieldOrNull(fragmentObject, "text");
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return (text.Trim(), NumberField(fragmentObject, "y"));
            }

            foreach (var child in EnumerateText(ArrayField(fragmentObject, "children")))
            {
                yield return child;
            }
        }
    }

    private static DiagnosticArray ArrayField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticArray>();

    private static double NumberField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static double NumberField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static string? StringFieldOrNull(DiagnosticObject value, string fieldName) =>
        value[fieldName] is DiagnosticStringValue stringValue ? stringValue.Value : null;
}
