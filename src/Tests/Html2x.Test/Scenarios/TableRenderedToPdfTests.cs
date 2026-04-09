using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Shouldly;
using Xunit.Abstractions;

namespace Html2x.Test.Scenarios;

public class TableRenderedToPdfTests(ITestOutputHelper output) : IntegrationTestBase(output)
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
    public async Task SimpleTableMarkup_ShouldEmitSupportedTableDiagnosticsAndPreserveRowMajorTextOrder()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body style='margin:0'>
                <table style='width: 400px; border: 1px solid black;'>
                  <tr>
                    <td style='padding: 10px; border: 1px solid black;'>A</td>
                    <td style='padding: 10px; border: 1px solid black;'>B</td>
                  </tr>
                  <tr>
                    <td style='padding: 10px; border: 1px solid black;'>C</td>
                    <td style='padding: 10px; border: 1px solid black;'>D</td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        var converter = new HtmlConverter();
        var result = await converter.ToPdfAsync(html, DefaultOptions);

        var diagnostics = GetDiagnostics(result);
        var page = GetLayoutPageSnapshot(result);
        var orderedTexts = EnumerateOrderedTexts(page.Fragments).ToList();
        var tableFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table").ToList();
        var rowFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table-row").ToList();
        var cellFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table-cell").ToList();

        diagnostics.ShouldNotBeNull();
        result.PdfBytes.ShouldNotBeEmpty();
        page.ShouldNotBeNull();
        diagnostics.Events.Any(e => e.Name == "layout/table").ShouldBeTrue();
        orderedTexts.ShouldBe(["A", "B", "C", "D"]);
        tableFragments.Count.ShouldBe(1);
        tableFragments[0].DerivedColumnCount.ShouldBe(2);
        rowFragments.Count.ShouldBe(2);
        rowFragments.Select(fragment => fragment.RowIndex).ShouldBe([0, 1]);
        cellFragments.Count.ShouldBe(4);
        cellFragments.Select(fragment => fragment.ColumnIndex).ShouldBe([0, 1, 0, 1]);
        cellFragments.All(static fragment => fragment.Size.Height > 30f).ShouldBeTrue();
        cellFragments[0].Y.ShouldBe(cellFragments[1].Y, 0.01f);
        cellFragments[2].Y.ShouldBe(cellFragments[3].Y, 0.01f);
        cellFragments[2].Y.ShouldBeGreaterThan(cellFragments[0].Y);
    }

    [Fact]
    public async Task TableInsideBlockContainer_ShouldAppearInLayoutSnapshot()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body style='margin:0'>
                <div>
                  <h2>Wrapped Table</h2>
                  <table style='width: 400px; border: 1px solid black;'>
                    <tr>
                      <td style='padding: 10px; border: 1px solid black;'>A</td>
                      <td style='padding: 10px; border: 1px solid black;'>B</td>
                    </tr>
                  </table>
                </div>
              </body>
            </html>
            """;

        var converter = new HtmlConverter();
        var result = await converter.ToPdfAsync(html, DefaultOptions);

        var diagnostics = GetDiagnostics(result);
        var page = GetLayoutPageSnapshot(result);
        var orderedTexts = EnumerateOrderedTexts(page.Fragments).ToList();
        var tableFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table").ToList();
        var cellFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table-cell").ToList();

        diagnostics.Events.Any(e => e.Name == "layout/table").ShouldBeTrue();
        tableFragments.Count.ShouldBe(1);
        cellFragments.Count.ShouldBe(2);
        orderedTexts.ShouldContain("Wrapped Table");
        orderedTexts.ShouldContain("A");
        orderedTexts.ShouldContain("B");
    }

    [Fact]
    public async Task HeaderStyledTable_ShouldPreserveHeaderIdentityAndColumnAlignment()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body style='margin:0'>
                <table style='width: 400px; border: 1px solid black;'>
                  <tr style='background-color: #d1d5db;'>
                    <th style='padding: 10px; border: 1px solid black;'>Name</th>
                    <th style='padding: 10px; border: 1px solid black;'>Status</th>
                  </tr>
                  <tr>
                    <td style='padding: 10px; border: 1px solid black;'>Alpha</td>
                    <td style='padding: 10px; border: 1px solid black; background-color: #fef3c7;'>Ready</td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        var converter = new HtmlConverter();
        var result = await converter.ToPdfAsync(html, DefaultOptions);

        var diagnostics = GetDiagnostics(result);
        var page = GetLayoutPageSnapshot(result);
        var orderedTexts = EnumerateOrderedTexts(page.Fragments).ToList();
        var tableFragment = Flatten(page.Fragments)
            .Single(static fragment => fragment.Kind == "table");
        var rows = tableFragment.Children.Where(static fragment => fragment.Kind == "table-row").ToList();

        diagnostics.Events.Any(e => e.Name == "layout/table").ShouldBeTrue();
        result.PdfBytes.ShouldNotBeEmpty();
        orderedTexts.ShouldBe(["Name", "Status", "Alpha", "Ready"]);
        tableFragment.DerivedColumnCount.ShouldBe(2);
        rows.Count.ShouldBe(2);
        rows.Select(row => row.RowIndex).ShouldBe([0, 1]);

        var headerCells = rows[0].Children.Where(static fragment => fragment.Kind == "table-cell").ToList();
        var bodyCells = rows[1].Children.Where(static fragment => fragment.Kind == "table-cell").ToList();

        headerCells.Count.ShouldBe(2);
        bodyCells.Count.ShouldBe(2);
        headerCells.All(static cell => cell.IsHeader == true).ShouldBeTrue();
        bodyCells.All(static cell => cell.IsHeader == false).ShouldBeTrue();
        headerCells[0].X.ShouldBe(bodyCells[0].X, 0.01f);
        headerCells[1].X.ShouldBe(bodyCells[1].X, 0.01f);
        headerCells[0].Size.Width.ShouldBe(bodyCells[0].Size.Width, 0.01f);
        headerCells[1].Size.Width.ShouldBe(bodyCells[1].Size.Width, 0.01f);
        bodyCells[1].ColumnIndex.ShouldBe(1);
        bodyCells[1].Y.ShouldBeGreaterThan(headerCells[1].Y);
    }

    [Fact]
    public async Task TableWithColspan_ShouldEmitUnsupportedStructureDiagnosticsAndSkipTableFragments()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body style='margin:0'>
                <table style='width: 400px; border: 1px solid black;'>
                  <tr>
                    <td colspan='2' style='padding: 10px; border: 1px solid black;'>A</td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        var converter = new HtmlConverter();
        var result = await converter.ToPdfAsync(html, DefaultOptions);

        var diagnostics = GetDiagnostics(result);
        var page = GetLayoutPageSnapshot(result);
        var tableFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table").ToList();
        var rowFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table-row").ToList();
        var cellFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table-cell").ToList();
        var unsupportedEvent = diagnostics.Events.FirstOrDefault(e => e.Name == "layout/table/unsupported-structure");
        var tableLayoutEvent = diagnostics.Events.FirstOrDefault(e => e.Name == "layout/table");

        result.PdfBytes.ShouldNotBeEmpty();
        unsupportedEvent.ShouldNotBeNull();
        unsupportedEvent!.Payload.ShouldBeOfType<UnsupportedStructurePayload>();
        ((UnsupportedStructurePayload)unsupportedEvent.Payload!).StructureKind.ShouldBe("colspan");
        tableLayoutEvent.ShouldNotBeNull();
        tableLayoutEvent!.Payload.ShouldBeOfType<TableLayoutPayload>();
        ((TableLayoutPayload)tableLayoutEvent.Payload!).Outcome.ShouldBe("Unsupported");
        ((TableLayoutPayload)tableLayoutEvent.Payload!).Reason.ShouldBe("Table cell colspan is not supported.");
        tableFragments.Count.ShouldBe(1);
        tableFragments[0].Size.Height.ShouldBe(0f);
        rowFragments.ShouldBeEmpty();
        cellFragments.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnsupportedTable_ShouldReportDiagnosticsAndPreserveSurroundingTextOrder()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body style='margin:0'>
                <p>before table</p>
                <table style='width: 400px; border: 1px solid black;'>
                  <tr>
                    <td colspan='2' style='padding: 10px; border: 1px solid black;'>merged cell</td>
                  </tr>
                </table>
                <p>after table</p>
              </body>
            </html>
            """;

        var converter = new HtmlConverter();
        var result = await converter.ToPdfAsync(html, DefaultOptions);

        var diagnostics = GetDiagnostics(result);
        var page = GetLayoutPageSnapshot(result);
        var orderedTexts = EnumerateOrderedTexts(page.Fragments).ToList();
        var tableFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table").ToList();
        var rowFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table-row").ToList();
        var cellFragments = Flatten(page.Fragments).Where(static fragment => fragment.Kind == "table-cell").ToList();
        var unsupportedEvent = diagnostics.Events.FirstOrDefault(e => e.Name == "layout/table/unsupported-structure");
        var tableLayoutEvent = diagnostics.Events.FirstOrDefault(e => e.Name == "layout/table");

        result.PdfBytes.ShouldNotBeEmpty();
        unsupportedEvent.ShouldNotBeNull();
        unsupportedEvent!.Payload.ShouldBeOfType<UnsupportedStructurePayload>();
        ((UnsupportedStructurePayload)unsupportedEvent.Payload!).StructureKind.ShouldBe("colspan");
        tableLayoutEvent.ShouldNotBeNull();
        tableLayoutEvent!.Payload.ShouldBeOfType<TableLayoutPayload>();

        var tablePayload = (TableLayoutPayload)tableLayoutEvent.Payload!;
        tablePayload.Outcome.ShouldBe("Unsupported");
        tablePayload.RowCount.ShouldBe(1);
        tablePayload.Reason.ShouldBe("Table cell colspan is not supported.");

        orderedTexts.ShouldBe(["before table", "after table"]);
        tableFragments.Count.ShouldBe(1);
        tableFragments[0].Size.Height.ShouldBe(0f);
        rowFragments.ShouldBeEmpty();
        cellFragments.ShouldBeEmpty();
    }

    [Fact]
    public void EnumerateOrderedTexts_NestedFragments_PreservesTraversalOrder()
    {
        var fragments = new[]
        {
            new FragmentSnapshot
            {
                Kind = "block",
                Children =
                [
                    new FragmentSnapshot { Kind = "text", Text = "A" },
                    new FragmentSnapshot
                    {
                        Kind = "block",
                        Children =
                        [
                            new FragmentSnapshot { Kind = "text", Text = "B" },
                            new FragmentSnapshot { Kind = "text", Text = "C" }
                        ]
                    },
                    new FragmentSnapshot { Kind = "text", Text = "D" }
                ]
            }
        };

        var orderedTexts = EnumerateOrderedTexts(fragments).ToList();

        orderedTexts.ShouldBe(["A", "B", "C", "D"]);
    }

    private static DiagnosticsSession GetDiagnostics(Html2PdfResult result)
    {
        result.Diagnostics.ShouldNotBeNull();
        return result.Diagnostics!;
    }

    private static LayoutPageSnapshot GetLayoutPageSnapshot(Html2PdfResult result)
    {
        var diagnostics = GetDiagnostics(result);
        var endLayoutBuild = diagnostics.Events.FirstOrDefault(x =>
            x is { Type: DiagnosticsEventType.EndStage, Payload: not null, Name: "LayoutBuild" });

        endLayoutBuild.ShouldNotBeNull();
        endLayoutBuild!.Payload.ShouldBeOfType<LayoutSnapshotPayload>();

        var snapshot = ((LayoutSnapshotPayload)endLayoutBuild.Payload).Snapshot;
        snapshot.Pages.ShouldNotBeEmpty();

        return snapshot.Pages[0];
    }

    private static IEnumerable<string> EnumerateOrderedTexts(IReadOnlyList<FragmentSnapshot> fragments)
    {
        foreach (var fragment in fragments)
        {
            if (!string.IsNullOrWhiteSpace(fragment.Text))
            {
                yield return fragment.Text!.Trim();
            }

            foreach (var childText in EnumerateOrderedTexts(fragment.Children))
            {
                if (!string.IsNullOrWhiteSpace(childText))
                {
                    yield return childText;
                }
            }
        }
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
