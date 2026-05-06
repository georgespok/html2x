using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Test.TestHelpers;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.Text;
using Shouldly;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test;

public class LayoutBuilderTests
{
    private static readonly string[] LayoutStageNames =
    [
        "stage/dom",
        "stage/style",
        "stage/box-tree",
        "stage/fragment-tree",
        "stage/pagination"
    ];

    [Fact]
    public async Task BuildAsync_ParagraphText_ReturnsPaginatedLayoutContainingText()
    {
        const string html = @"
            <html>
              <body style='margin: 10pt;'>
                <p>Hello pipeline</p>
              </body>
            </html>";

        var layout = await CreateBuilder().BuildAsync(html, new() { PageSize = PaperSizes.Letter });

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Margins.Top.ShouldBe(10f);
        EnumerateText(layout).ShouldContain("Hello pipeline");
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSinkIsProvided_PublishesStageLifecycleRecords()
    {
        const string html = "<html><body><p>diagnostics</p></body></html>";
        var diagnosticsSink = new RecordingDiagnosticsSink();

        await CreateBuilder().BuildAsync(html, new() { PageSize = PaperSizes.A4 }, diagnosticsSink);

        foreach (var stageName in LayoutStageNames)
        {
            diagnosticsSink.Records.ShouldContain(e =>
                e.Stage == stageName &&
                e.Name == "stage/started" &&
                e.Severity == DiagnosticSeverity.Info);
            diagnosticsSink.Records.ShouldContain(e =>
                e.Stage == stageName &&
                e.Name == "stage/succeeded" &&
                e.Severity == DiagnosticSeverity.Info);
        }
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSinkIsProvided_PublishesGeometrySnapshotRecord()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 100pt; height: 20pt;'>snapshot</div>
              </body>
            </html>";
        var diagnosticsSink = new RecordingDiagnosticsSink();

        await CreateBuilder().BuildAsync(html, new() { PageSize = PaperSizes.Letter }, diagnosticsSink);

        var geometryEvent = diagnosticsSink.Records
            .SingleOrDefault(static e => e.Name == "layout/geometry-snapshot");

        geometryEvent.ShouldNotBeNull();
        var snapshot = geometryEvent.Fields["snapshot"].ShouldBeOfType<DiagnosticObject>();
        snapshot["boxes"].ShouldBeOfType<DiagnosticArray>().Count.ShouldBeGreaterThan(0);
        snapshot["fragments"].ShouldBeOfType<DiagnosticObject>()["pageCount"].ShouldBe(new DiagnosticNumberValue(1));
        var pagination = snapshot["pagination"].ShouldBeOfType<DiagnosticArray>();
        pagination.Count.ShouldBe(1);
        var placements = pagination[0].ShouldBeOfType<DiagnosticObject>()["placements"]
            .ShouldBeOfType<DiagnosticArray>();
        placements.Count.ShouldBeGreaterThan(0);
        placements[0].ShouldBeOfType<DiagnosticObject>()["decisionKind"].ShouldBe(new DiagnosticStringValue("Placed"));
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSinkIsProvided_PublishesGeometrySnapshotBeforePaginationSucceeds()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <p>snapshot order</p>
              </body>
            </html>";
        var diagnosticsSink = new RecordingDiagnosticsSink();

        await CreateBuilder().BuildAsync(html, new() { PageSize = PaperSizes.Letter }, diagnosticsSink);

        var indexedRecords = diagnosticsSink.Records
            .Select((record, index) => new { Record = record, Index = index })
            .ToArray();
        var snapshotIndex = indexedRecords.Single(static item =>
            item.Record.Stage == "stage/pagination" &&
            item.Record.Name == "layout/geometry-snapshot").Index;
        var paginationSucceededIndex = indexedRecords.Single(static item =>
            item.Record.Stage == "stage/pagination" &&
            item.Record.Name == "stage/succeeded").Index;

        snapshotIndex.ShouldBeLessThan(paginationSucceededIndex);
    }

    [Fact]
    public async Task BuildAsync_UnsupportedInlineBlockTable_PublishesBoxTreeFailure()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 320pt;'>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    <table>
                      <tr><td>unsupported</td></tr>
                    </table>
                  </span>
                </div>
              </body>
            </html>";
        var diagnosticsSink = new RecordingDiagnosticsSink();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await CreateBuilder().BuildAsync(html, new() { PageSize = PaperSizes.A4 }, diagnosticsSink));

        diagnosticsSink.Records.ShouldContain(e =>
            e.Stage == "stage/box-tree" &&
            e.Name == "stage/started");
        diagnosticsSink.Records.ShouldContain(e =>
            e.Stage == "stage/box-tree" &&
            e.Name == "stage/failed" &&
            e.Severity == DiagnosticSeverity.Error);
        diagnosticsSink.Records.ShouldNotContain(e =>
            e.Stage == "stage/fragment-tree" &&
            e.Name == "stage/started");
    }

    private static LayoutBuilder CreateBuilder(
        ITextMeasurer? textMeasurer = null,
        IImageMetadataResolver? imageMetadataResolver = null) =>
        new(
            textMeasurer ?? InlineFlowTestHelpers.CreateLinearMeasurer(6f),
            imageMetadataResolver ?? new NoopImageMetadataResolver());

    private static IEnumerable<string> EnumerateText(HtmlLayout layout)
    {
        return layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Select(static run => run.Text);
    }

    private static IEnumerable<LayoutFragment> EnumerateFragments(LayoutFragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        foreach (var nested in EnumerateFragments(child))
        {
            yield return nested;
        }
    }
}