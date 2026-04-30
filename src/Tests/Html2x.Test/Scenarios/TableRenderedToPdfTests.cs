using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
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
    public async Task SimpleTableMarkup_EmitsDiagnosticsAndRowMajorText()
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
        diagnostics.Records.Any(e => e.Name == "layout/table").ShouldBeTrue();
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
    public async Task TableInsideBlockContainer_AppearInLayoutSnapshot()
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

        diagnostics.Records.Any(e => e.Name == "layout/table").ShouldBeTrue();
        tableFragments.Count.ShouldBe(1);
        cellFragments.Count.ShouldBe(2);
        orderedTexts.ShouldContain("Wrapped Table");
        orderedTexts.ShouldContain("A");
        orderedTexts.ShouldContain("B");
    }

    [Fact]
    public async Task HeaderStyledTable_PreserveHeaderIdentityAndColumnAlignment()
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

        diagnostics.Records.Any(e => e.Name == "layout/table").ShouldBeTrue();
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
    public async Task TableWithColspan_RejectsUnsupportedStructure()
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
        var unsupportedEvent = diagnostics.Records.FirstOrDefault(e => e.Name == "layout/table/unsupported-structure");
        var tableLayoutEvent = diagnostics.Records.FirstOrDefault(e => e.Name == "layout/table");

        result.PdfBytes.ShouldNotBeEmpty();
        unsupportedEvent.ShouldNotBeNull();
        StringField(unsupportedEvent!, "structureKind").ShouldBe("colspan");
        tableLayoutEvent.ShouldNotBeNull();
        StringField(tableLayoutEvent!, "outcome").ShouldBe("Unsupported");
        StringField(tableLayoutEvent, "reason").ShouldBe("Table cell colspan is not supported.");
        tableFragments.Count.ShouldBe(1);
        tableFragments[0].Size.Height.ShouldBe(0f);
        rowFragments.ShouldBeEmpty();
        cellFragments.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnsupportedTable_ReportDiagnosticsAndPreserveSurroundingTextOrder()
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
        var unsupportedEvent = diagnostics.Records.FirstOrDefault(e => e.Name == "layout/table/unsupported-structure");
        var tableLayoutEvent = diagnostics.Records.FirstOrDefault(e => e.Name == "layout/table");

        result.PdfBytes.ShouldNotBeEmpty();
        unsupportedEvent.ShouldNotBeNull();
        StringField(unsupportedEvent!, "structureKind").ShouldBe("colspan");
        tableLayoutEvent.ShouldNotBeNull();
        StringField(tableLayoutEvent!, "outcome").ShouldBe("Unsupported");
        NumberField(tableLayoutEvent, "rowCount").ShouldBe(1);
        StringField(tableLayoutEvent, "reason").ShouldBe("Table cell colspan is not supported.");

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

    private static DiagnosticsReport GetDiagnostics(Html2PdfResult result)
    {
        result.DiagnosticsReport.ShouldNotBeNull();
        return result.DiagnosticsReport!;
    }

    private static LayoutPageSnapshot GetLayoutPageSnapshot(Html2PdfResult result)
    {
        var diagnostics = GetDiagnostics(result);
        var endLayoutBuild = diagnostics.Records.FirstOrDefault(x =>
            x is { Stage: "LayoutBuild", Name: "stage/succeeded" });

        endLayoutBuild.ShouldNotBeNull();
        var snapshot = endLayoutBuild!.Fields["snapshot"].ShouldBeOfType<DiagnosticObject>();
        var pages = ArrayField(snapshot, "pages");
        pages.ShouldNotBeEmpty();

        return MapPage(pages[0].ShouldBeOfType<DiagnosticObject>());
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

    private static LayoutPageSnapshot MapPage(DiagnosticObject page) =>
        new()
        {
            Fragments = ArrayField(page, "fragments")
                .Select(static fragment => MapFragment(fragment.ShouldBeOfType<DiagnosticObject>()))
                .ToList()
        };

    private static FragmentSnapshot MapFragment(DiagnosticObject fragment) =>
        new()
        {
            Kind = StringFieldOrEmpty(fragment, "kind"),
            Text = StringFieldOrNull(fragment, "text"),
            X = (float)NumberField(fragment, "x"),
            Y = (float)NumberField(fragment, "y"),
            Size = MapSize(fragment["size"].ShouldBeOfType<DiagnosticObject>()),
            DerivedColumnCount = NullableIntField(fragment, "derivedColumnCount"),
            RowIndex = NullableIntField(fragment, "rowIndex"),
            ColumnIndex = NullableIntField(fragment, "columnIndex"),
            IsHeader = NullableBoolField(fragment, "isHeader"),
            Children = ArrayField(fragment, "children")
                .Select(static child => MapFragment(child.ShouldBeOfType<DiagnosticObject>()))
                .ToList()
        };

    private static SizePt MapSize(DiagnosticObject size) =>
        new((float)NumberField(size, "width"), (float)NumberField(size, "height"));

    private static DiagnosticArray ArrayField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticArray>();

    private static double NumberField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static double NumberField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static string StringField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticStringValue>().Value;

    private static string StringFieldOrEmpty(DiagnosticObject value, string fieldName) =>
        StringFieldOrNull(value, fieldName) ?? string.Empty;

    private static string? StringFieldOrNull(DiagnosticObject value, string fieldName) =>
        value[fieldName] is DiagnosticStringValue stringValue ? stringValue.Value : null;

    private static int? NullableIntField(DiagnosticObject value, string fieldName) =>
        value[fieldName] is DiagnosticNumberValue number ? (int)number.Value : null;

    private static bool? NullableBoolField(DiagnosticObject value, string fieldName) =>
        value[fieldName] is DiagnosticBooleanValue boolean ? boolean.Value : null;

    private sealed class LayoutPageSnapshot
    {
        public IReadOnlyList<FragmentSnapshot> Fragments { get; init; } = [];
    }

    private sealed class FragmentSnapshot
    {
        public string Kind { get; init; } = string.Empty;
        public string? Text { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public SizePt Size { get; init; }
        public int? DerivedColumnCount { get; init; }
        public int? RowIndex { get; init; }
        public int? ColumnIndex { get; init; }
        public bool? IsHeader { get; init; }
        public IReadOnlyList<FragmentSnapshot> Children { get; init; } = [];
    }
}
