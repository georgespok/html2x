using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

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

        var layout = await CreateBuilder().BuildAsync(html, new LayoutOptions { PageSize = PaperSizes.Letter });

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Margins.Top.ShouldBe(10f);
        EnumerateText(layout).ShouldContain("Hello pipeline");
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSessionIsProvided_PublishesStageLifecycleEvents()
    {
        const string html = "<html><body><p>diagnostics</p></body></html>";
        var diagnosticsSession = new DiagnosticsSession();

        await CreateBuilder().BuildAsync(html, new LayoutOptions { PageSize = PaperSizes.A4 }, diagnosticsSession);

        foreach (var stageName in LayoutStageNames)
        {
            diagnosticsSession.Events.ShouldContain(e =>
                e.Name == stageName &&
                e.Type == DiagnosticsEventType.StartStage &&
                e.StageState == DiagnosticStageState.Started);
            diagnosticsSession.Events.ShouldContain(e =>
                e.Name == stageName &&
                e.Type == DiagnosticsEventType.EndStage &&
                e.StageState == DiagnosticStageState.Succeeded);
        }
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSessionIsProvided_PublishesGeometrySnapshotPayload()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 100pt; height: 20pt;'>snapshot</div>
              </body>
            </html>";
        var diagnosticsSession = new DiagnosticsSession();

        await CreateBuilder().BuildAsync(html, new LayoutOptions { PageSize = PaperSizes.Letter }, diagnosticsSession);

        var geometryEvent = diagnosticsSession.Events
            .SingleOrDefault(static e => e.Name == "layout/geometry-snapshot");

        geometryEvent.ShouldNotBeNull();
        var payload = geometryEvent!.Payload.ShouldBeOfType<GeometrySnapshotPayload>();
        payload.Snapshot.Boxes.Count.ShouldBeGreaterThan(0);
        payload.Snapshot.Fragments.PageCount.ShouldBe(1);
        payload.Snapshot.Pagination.Count.ShouldBe(1);
        payload.Snapshot.Pagination[0].Placements.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task BuildAsync_UnsupportedInlineBlockTable_PublishesFailedBoxTreeStageAndRethrows()
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
        var diagnosticsSession = new DiagnosticsSession();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await CreateBuilder().BuildAsync(html, new LayoutOptions { PageSize = PaperSizes.A4 }, diagnosticsSession));

        diagnosticsSession.Events.ShouldContain(e =>
            e.Name == "stage/box-tree" &&
            e.Type == DiagnosticsEventType.StartStage &&
            e.StageState == DiagnosticStageState.Started);
        diagnosticsSession.Events.ShouldContain(e =>
            e.Name == "stage/box-tree" &&
            e.Type == DiagnosticsEventType.Error &&
            e.StageState == DiagnosticStageState.Failed);
        diagnosticsSession.Events.ShouldNotContain(e =>
            e.Name == "stage/fragment-tree" &&
            e.Type == DiagnosticsEventType.StartStage);
    }

    private static LayoutBuilder CreateBuilder(
        ITextMeasurer? textMeasurer = null,
        IFontSource? fontSource = null,
        IImageProvider? imageProvider = null)
    {
        return new LayoutBuilder(
            textMeasurer ?? InlineFlowTestHelpers.CreateLinearMeasurer(6f),
            fontSource ?? LayoutBuilderFixture.CreateFontSource(),
            imageProvider ?? new NoopImageProvider());
    }

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
