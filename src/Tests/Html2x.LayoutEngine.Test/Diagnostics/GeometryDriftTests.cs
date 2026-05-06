using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Test.TestHelpers;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Diagnostics;

/// <summary>
///     Verifies geometry snapshots stay aligned with the ownership contract.
/// </summary>
public sealed class GeometryDriftTests
{
    [Fact]
    public async Task Build_PublishedLayoutCharacterizesMixedGeometryFactsAndSourceOrder()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div id='mixed' style='margin: 0;'>
                  Alpha
                  <span id='chip' style='display: inline-block; padding: 1pt;'>Chip</span>
                  Omega
                </div>
                <ul style='margin: 0; padding: 0;'>
                  <li style='margin: 0;'>One</li>
                </ul>
                <hr style='margin: 0; padding: 2pt;' />
                <img id='hero' src='hero.png' width='20' height='10' style='display: block; margin: 0;' />
                <table id='supported' style='margin: 0; width: 160px;'>
                  <tr><th>A</th><td>B</td></tr>
                </table>
                <table id='unsupported' style='margin: 0; width: 160px;'>
                  <tr><td colspan='2'>C</td></tr>
                </table>
              </body>
            </html>
            """);

        var blocks = result.PublishedLayout.Blocks;
        blocks.Select(static block => block.Identity.ElementIdentity)
            .ShouldBe(["div#mixed", "ul", "hr", "img#hero", "table#supported", "table#unsupported"]);
        blocks.Select(static block => block.Identity.SourceOrder)
            .ShouldBe(blocks.Select(static block => block.Identity.SourceOrder).Order().ToArray());
        blocks.Select(static block => block.Identity.SourceOrder)
            .Distinct()
            .Count()
            .ShouldBe(blocks.Count);

        var mixed = blocks[0];
        var mixedItems = mixed.InlineLayout
            .ShouldNotBeNull()
            .Segments
            .ShouldHaveSingleItem()
            .Lines
            .ShouldHaveSingleItem()
            .Items;
        mixedItems.Select(static item => item.Order).ShouldBe([0, 1, 2]);
        mixedItems[0].ShouldBeOfType<PublishedInlineTextItem>()
            .Runs
            .ShouldHaveSingleItem()
            .Text
            .ShouldContain("Alpha");
        mixedItems[1].ShouldBeOfType<PublishedInlineObjectItem>()
            .Content
            .Display
            .FormattingContext
            .ShouldBe(FormattingContextKind.InlineBlock);
        mixedItems[2].ShouldBeOfType<PublishedInlineTextItem>()
            .Runs
            .ShouldHaveSingleItem()
            .Text
            .ShouldContain("Omega");

        var listItem = blocks[1].Children.ShouldHaveSingleItem();
        listItem.Display.MarkerOffset.ShouldBe(12f);
        var listRuns = listItem.InlineLayout
            .ShouldNotBeNull()
            .Segments
            .ShouldHaveSingleItem()
            .Lines
            .ShouldHaveSingleItem()
            .Items
            .OfType<PublishedInlineTextItem>()
            .SelectMany(static item => item.Runs)
            .Select(static run => run.Text)
            .ToArray();
        string.Concat(listRuns).ShouldContain("One");

        blocks[2].Rule.ShouldNotBeNull();
        blocks[3].Image.ShouldNotBeNull().Src.ShouldBe("hero.png");

        var supportedTable = blocks[4];
        supportedTable.Table.ShouldNotBeNull().DerivedColumnCount.ShouldBe(2);
        supportedTable.Children.ShouldHaveSingleItem()
            .Table
            .ShouldNotBeNull()
            .RowIndex
            .ShouldBe(0);
        supportedTable.Children[0].Children[0].Table.ShouldNotBeNull().ColumnIndex.ShouldBe(0);
        supportedTable.Children[0].Children[0].Table!.IsHeader.ShouldBe(true);
        supportedTable.Children[0].Children[1].Table.ShouldNotBeNull().ColumnIndex.ShouldBe(1);
        supportedTable.Children[0].Children[1].Table!.IsHeader.ShouldBe(false);

        var unsupportedTable = blocks[5];
        unsupportedTable.Table.ShouldNotBeNull().DerivedColumnCount.ShouldBe(0);
        unsupportedTable.Geometry.Height.ShouldBe(0f);
        unsupportedTable.Children.ShouldBeEmpty();
        result.Diagnostics
            .Any(static record =>
                record.Name == "layout/table" &&
                record.Fields.ContainsKey("outcome") &&
                record.Fields["outcome"] is DiagnosticStringValue { Value: "Unsupported" })
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Build_GeometrySnapshotPreservesFragmentMetadataOwnerAndConsumers()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <ul style='margin: 0; padding: 0;'>
                  <li style='margin: 0;'>One</li>
                </ul>
                <table style='margin: 0; width: 160px;'>
                  <tr>
                    <th>A</th>
                    <td>B</td>
                  </tr>
                </table>
              </body>
            </html>
            """);

        var fragmentSnapshots = result.Snapshot.Fragments.Pages
            .SelectMany(static page => Flatten(page.Fragments))
            .Where(static fragment => fragment.Kind is "block" or "table" or "table-row" or "table-cell")
            .ToList();

        fragmentSnapshots.ShouldNotBeEmpty();
        fragmentSnapshots.ShouldAllBe(static fragment =>
            fragment.MetadataOwner == "FragmentBuilder" &&
            fragment.MetadataConsumer == "LayoutSnapshotMapper");

        var table = fragmentSnapshots.First(static fragment => fragment.Kind == "table");
        var row = fragmentSnapshots.First(static fragment => fragment.Kind == "table-row");
        var header =
            fragmentSnapshots.First(static fragment => fragment.Kind == "table-cell" && fragment.IsHeader == true);

        table.DerivedColumnCount.ShouldBe(2);
        row.RowIndex.ShouldBe(0);
        header.ColumnIndex.ShouldBe(0);

        var boxSnapshots = result.Snapshot.Boxes
            .SelectMany(FlattenBoxes)
            .Where(static box => box.DerivedColumnCount.HasValue || box.RowIndex.HasValue || box.ColumnIndex.HasValue)
            .ToList();

        boxSnapshots.ShouldNotBeEmpty();
        boxSnapshots.ShouldAllBe(static box =>
            box.MetadataOwner == "BlockLayoutEngine" &&
            box.MetadataConsumer == "GeometrySnapshotMapper");

        var placement = result.Snapshot.Pagination
            .SelectMany(static page => page.Placements)
            .First(static item => item.Kind == "Table");

        placement.MetadataOwner.ShouldBe("FragmentBuilder");
        placement.MetadataConsumer.ShouldBe("Pagination");
        placement.DisplayRole.ShouldBe(FragmentDisplayRole.Table);
        placement.FormattingContext.ShouldBe(FormattingContextKind.Block);
        placement.DerivedColumnCount.ShouldBe(2);
    }

    [Fact]
    public async Task Build_GeometrySnapshotCarriesSourceIdentityWithoutChangingLayoutPath()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div id='main' style='margin: 0;'>Alpha</div>
              </body>
            </html>
            """);

        var box = result.Snapshot.Boxes.ShouldHaveSingleItem();

        box.Path.ShouldBe("body/div");
        box.SourceNodeId.ShouldNotBeNull().ShouldBeGreaterThan(0);
        box.SourceContentId.ShouldBeNull();
        box.SourcePath.ShouldBe("body[0]/div[0]");
        box.SourceOrder.ShouldNotBeNull().ShouldBeGreaterThan(0);
        box.SourceElementIdentity.ShouldBe("div#main");
        box.GeneratedSourceKind.ShouldBeNull();
    }

    [Fact]
    public void Paginate_MetadataRichSubtree_PreservesGeometryAndMetadata()
    {
        var style = new VisualStyle(
            new ColorRgba(240, 240, 240, 255),
            BorderEdges.Uniform(new(1f, ColorRgba.Black, BorderLineStyle.Solid)),
            Padding: new Spacing(1f, 2f, 3f, 4f));
        var font = new FontKey("Arial", FontWeight.W400, FontStyle.Normal);
        var line = new LineBoxFragment
        {
            FragmentId = 500,
            PageNumber = 1,
            Rect = new(12f, 120f, 25f, 14f),
            ZOrder = 5,
            Style = style,
            BaselineY = 130f,
            LineHeight = 14f,
            TextAlign = "center",
            Runs =
            [
                new(
                    "A",
                    font,
                    12f,
                    new(12f, 130f),
                    10f,
                    9f,
                    3f,
                    TextDecorations.Underline,
                    ColorRgba.Black)
            ]
        };
        var image = new ImageFragment
        {
            FragmentId = 501,
            PageNumber = 1,
            Rect = new(40f, 121f, 30f, 20f),
            ContentRect = new(42f, 123f, 26f, 16f),
            ZOrder = 6,
            Style = style,
            Src = "image.png",
            AuthoredSizePx = new(30d, 20d),
            IntrinsicSizePx = new(60d, 40d),
            Status = ImageLoadStatus.Missing
        };
        var rule = new RuleFragment
        {
            FragmentId = 502,
            PageNumber = 1,
            Rect = new(10f, 145f, 80f, 2f),
            ZOrder = 7,
            Style = style
        };
        var cell = new TableCellFragment([line, image, rule])
        {
            FragmentId = 400,
            PageNumber = 1,
            Rect = new(10f, 118f, 90f, 32f),
            ZOrder = 4,
            Style = style,
            DisplayRole = FragmentDisplayRole.TableCell,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 3f,
            ColumnIndex = 2,
            IsHeader = true
        };
        var row = new TableRowFragment([cell])
        {
            FragmentId = 300,
            PageNumber = 1,
            Rect = new(8f, 116f, 100f, 36f),
            ZOrder = 3,
            Style = style,
            DisplayRole = FragmentDisplayRole.TableRow,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 2f,
            RowIndex = 4
        };
        var table = new TableFragment([row])
        {
            FragmentId = 200,
            PageNumber = 1,
            Rect = new(6f, 114f, 110f, 40f),
            ZOrder = 2,
            Style = style,
            DisplayRole = FragmentDisplayRole.Table,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 1f,
            DerivedColumnCount = 3
        };
        var sourceBlock = new BlockFragment([table])
        {
            FragmentId = 100,
            PageNumber = 1,
            Rect = new(0f, 95f, 120f, 60f),
            ZOrder = 1,
            Style = style,
            DisplayRole = FragmentDisplayRole.Block,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 4f
        };

        var pagination = new LayoutPaginator().Paginate(
            [
                new()
                {
                    FragmentId = 1,
                    Rect = new(0f, 10f, 100f, 75f)
                },
                sourceBlock
            ],
            new()
            {
                PageSize = new(200f, 100f),
                Margin = new(10f, 10f, 10f, 10f)
            });

        var movedBlock = pagination.Layout.Pages[1].Children.ShouldHaveSingleItem().ShouldBeOfType<BlockFragment>();
        var deltaX = movedBlock.Rect.X - sourceBlock.Rect.X;
        var deltaY = movedBlock.Rect.Y - sourceBlock.Rect.Y;
        var movedTable = movedBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();
        var movedRow = movedTable.Rows.ShouldHaveSingleItem();
        var movedCell = movedRow.Cells.ShouldHaveSingleItem();
        var movedLine = movedCell.Children.OfType<LineBoxFragment>().ShouldHaveSingleItem();
        var movedImage = movedCell.Children.OfType<ImageFragment>().ShouldHaveSingleItem();
        var movedRule = movedCell.Children.OfType<RuleFragment>().ShouldHaveSingleItem();

        AssertCommonTranslation(sourceBlock, movedBlock, deltaX, deltaY, 2);
        AssertCommonTranslation(table, movedTable, deltaX, deltaY, 2);
        AssertCommonTranslation(row, movedRow, deltaX, deltaY, 2);
        AssertCommonTranslation(cell, movedCell, deltaX, deltaY, 2);
        AssertCommonTranslation(line, movedLine, deltaX, deltaY, 2);
        AssertCommonTranslation(image, movedImage, deltaX, deltaY, 2);
        AssertCommonTranslation(rule, movedRule, deltaX, deltaY, 2);

        movedBlock.DisplayRole.ShouldBe(sourceBlock.DisplayRole);
        movedBlock.FormattingContext.ShouldBe(sourceBlock.FormattingContext);
        movedBlock.MarkerOffset.ShouldBe(sourceBlock.MarkerOffset);
        movedTable.DerivedColumnCount.ShouldBe(table.DerivedColumnCount);
        movedRow.RowIndex.ShouldBe(row.RowIndex);
        movedCell.ColumnIndex.ShouldBe(cell.ColumnIndex);
        movedCell.IsHeader.ShouldBe(cell.IsHeader);
        movedLine.BaselineY.ShouldBe(line.BaselineY + deltaY);
        movedLine.LineHeight.ShouldBe(line.LineHeight);
        movedLine.TextAlign.ShouldBe(line.TextAlign);
        movedLine.Runs.ShouldHaveSingleItem().Origin
            .ShouldBe(new(line.Runs[0].Origin.X + deltaX, line.Runs[0].Origin.Y + deltaY));
        movedImage.ContentRect.ShouldBe(new(
            image.ContentRect.X + deltaX,
            image.ContentRect.Y + deltaY,
            image.ContentRect.Width,
            image.ContentRect.Height));
        movedImage.Src.ShouldBe(image.Src);
        movedImage.AuthoredSizePx.ShouldBe(image.AuthoredSizePx);
        movedImage.IntrinsicSizePx.ShouldBe(image.IntrinsicSizePx);
        movedImage.IsMissing.ShouldBe(image.IsMissing);
        movedImage.IsOversize.ShouldBe(image.IsOversize);
    }

    [Fact]
    public async Task Build_InlineBlockGeometry_PreservesBoxAndLineGeometry()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  before
                  <span style='display: inline-block; padding: 4pt; border: 2pt solid black;'>X</span>
                  after
                </div>
              </body>
            </html>
            """);

        var inlineBlockFragment = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .OfType<BlockFragment>()
            .First(static fragment =>
                fragment.FormattingContext == FormattingContextKind.InlineBlock &&
                ContainsText(fragment, "X"));
        var nestedLine = inlineBlockFragment.Children
            .OfType<LineBoxFragment>()
            .First(static line => line.Runs.Any(run => run.Text.Contains("X", StringComparison.Ordinal)));
        var nestedRun = nestedLine.Runs.ShouldHaveSingleItem();

        nestedLine.Rect.X.ShouldBe(inlineBlockFragment.Rect.X + 6f, 0.01f);
        nestedLine.Rect.Y.ShouldBe(inlineBlockFragment.Rect.Y + 6f, 0.01f);
        nestedRun.Origin.X.ShouldBe(nestedLine.Rect.X, 0.01f);
        nestedRun.Origin.Y.ShouldBe(nestedLine.BaselineY, 0.01f);
    }

    [Fact]
    public async Task Build_InlineBlockExplicitSize_AppliesStyleDimensions()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  <span style='display: inline-block; width: 40px; height: 20px; padding: 4px; border: 2px solid black;'>X</span>
                </div>
              </body>
            </html>
            """);

        var inlineBlockFragment = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .OfType<BlockFragment>()
            .First(static fragment => fragment.FormattingContext == FormattingContextKind.InlineBlock);
        var nestedLine = inlineBlockFragment.Children
            .OfType<LineBoxFragment>()
            .ShouldHaveSingleItem();

        inlineBlockFragment.Rect.Width.ShouldBe(39f, 0.01f);
        inlineBlockFragment.Rect.Height.ShouldBe(24f, 0.01f);
        nestedLine.Rect.Width.ShouldBe(30f, 0.01f);
        nestedLine.Rect.Height.ShouldBe(14.4f, 0.01f);
    }

    [Fact]
    public async Task Build_InlineBlockWithMinAndMaxDimensions_AppliesConstraints()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  <span style='display: inline-block; width: 80px; max-width: 40px; height: 10px; min-height: 20px; border: 1px solid black;'>X</span>
                </div>
              </body>
            </html>
            """);

        var inlineBlockFragment = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .OfType<BlockFragment>()
            .First(static fragment => fragment.FormattingContext == FormattingContextKind.InlineBlock);
        var nestedLine = inlineBlockFragment.Children
            .OfType<LineBoxFragment>()
            .ShouldHaveSingleItem();

        inlineBlockFragment.Rect.Width.ShouldBe(31.5f, 0.01f);
        nestedLine.Rect.Width.ShouldBe(30f, 0.01f);
        inlineBlockFragment.Rect.Height.ShouldBe(16.5f, 0.01f);
        nestedLine.Rect.Height.ShouldBe(14.4f, 0.01f);
    }

    [Theory]
    [MemberData(nameof(GetGoldenCases))]
    public async Task Build_GeometrySnapshotMatchesGoldenBaseline(
        string _,
        string html,
        string expectedSnapshot)
    {
        var result = await GeometryTestHarness.BuildAsync(html);

        GeometryInvariantValidator.AssertInvariants(result);

        GeometryTestHarness.NormalizeNewLines(GeometryTestHarness.RenderSnapshot(result.Snapshot))
            .ShouldBe(GeometryTestHarness.NormalizeNewLines(expectedSnapshot));
    }

    public static IEnumerable<object[]> GetGoldenCases()
    {
        yield return
        [
            "mixed-content",
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0; padding: 0;'>
                  Alpha
                  <p style='margin: 0;'>Beta</p>
                </div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,612,28.8 content=0,0,612,28.8 marker=0 anonymous=false inlineBlock=false
              2:block path=body/div/p rect=0,14.4,612,14.4 content=0,14.4,612,14.4 marker=0 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,612,28.8
                2:line rect=0,0,612,14.4 text="Alpha" occupied=0,0,10,14.4
                3:block rect=0,14.4,612,14.4
                  4:line rect=0,14.4,612,14.4 text="Beta" occupied=0,14.4,10,14.4
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block decision=Placed oversized=false rect=0,0,612,28.8
            """
        ];

        yield return
        [
            "list-items",
            """
            <html>
              <body style='margin: 0;'>
                <ul style='margin: 0; padding: 0;'>
                  <li style='margin: 0;'>One</li>
                  <li style='margin: 0;'>Two</li>
                </ul>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/ul rect=0,0,612,28.8 content=0,0,612,28.8 marker=0 anonymous=false inlineBlock=false
              2:listitem path=body/ul/li rect=0,0,612,14.4 content=0,0,612,14.4 marker=12 anonymous=false inlineBlock=false
              3:listitem path=body/ul/li rect=0,14.4,612,14.4 content=0,14.4,612,14.4 marker=12 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,612,28.8
                2:block rect=0,0,612,14.4 marker=12
                  3:line rect=12,0,600,14.4 text="• One" occupied=12,0,20,14.4
                4:block rect=0,14.4,612,14.4 marker=12
                  5:line rect=12,14.4,600,14.4 text="• Two" occupied=12,14.4,20,14.4
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block decision=Placed oversized=false rect=0,0,612,28.8
            """
        ];

        yield return
        [
            "table",
            """
            <html>
              <body style='margin: 0;'>
                <table style='margin: 0; width: 160px;'>
                  <tr>
                    <th>A</th>
                    <th>B</th>
                  </tr>
                  <tr>
                    <td>C</td>
                    <td>D</td>
                  </tr>
                </table>
              </body>
            </html>
            """,
            """
            boxes
            1:table path=body/table rect=0,0,120,40 content=0,0,120,40 marker=0 anonymous=false inlineBlock=false columns=2
              2:tablerow path=body/table/tbody/tr rect=0,0,120,20 content=0,0,120,20 marker=0 anonymous=false inlineBlock=false row=0
                3:tablecell path=body/table/tbody/tr/th rect=0,0,60,20 content=0,0,60,20 marker=0 anonymous=false inlineBlock=false column=0 header=true
                4:tablecell path=body/table/tbody/tr/th rect=60,0,60,20 content=60,0,60,20 marker=0 anonymous=false inlineBlock=false column=1 header=true
              5:tablerow path=body/table/tbody/tr rect=0,20,120,20 content=0,20,120,20 marker=0 anonymous=false inlineBlock=false row=1
                6:tablecell path=body/table/tbody/tr/td rect=0,20,60,20 content=0,20,60,20 marker=0 anonymous=false inlineBlock=false column=0 header=false
                7:tablecell path=body/table/tbody/tr/td rect=60,20,60,20 content=60,20,60,20 marker=0 anonymous=false inlineBlock=false column=1 header=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:table rect=0,0,120,40 columns=2
                2:table-row rect=0,0,120,20 row=0
                  3:table-cell rect=0,0,60,20 column=0 header=true
                    4:line rect=0,0,60,14.4 text="A" occupied=0,0,10,14.4
                  5:table-cell rect=60,0,60,20 column=1 header=true
                    6:line rect=60,0,60,14.4 text="B" occupied=60,0,10,14.4
                7:table-row rect=0,20,120,20 row=1
                  8:table-cell rect=0,20,60,20 column=0 header=false
                    9:line rect=0,20,60,14.4 text="C" occupied=0,20,10,14.4
                  10:table-cell rect=60,20,60,20 column=1 header=false
                    11:line rect=60,20,60,14.4 text="D" occupied=60,20,10,14.4
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Table decision=Placed oversized=false rect=0,0,120,40
            """
        ];

        yield return
        [
            "image",
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  <img src='image.png' width='40' height='20' style='padding: 4px; border: 2px solid black;' />
                </div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,612,24 content=0,0,612,24 marker=0 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,612,24
                2:image rect=0,0,39,24
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block decision=Placed oversized=false rect=0,0,612,24
            """
        ];

        yield return
        [
            "inline-block",
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  before
                  <span style='display: inline-block; width: 40px; height: 20px; border: 1px solid black;'>X</span>
                  after
                </div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,612,16.5 content=0,0,612,16.5 marker=0 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,612,16.5
                2:line rect=0,0,612,16.5 text="before" occupied=0,0,10,16.5
                3:block rect=10,0,31.5,16.5
                  4:line rect=10.75,0.75,30,14.4 text="X" occupied=10.75,0.75,10,14.4
                5:line rect=0,0,612,16.5 text=" after" occupied=41.5,0,10,16.5
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block decision=Placed oversized=false rect=0,0,612,16.5
            """
        ];

        yield return
        [
            "pagination",
            """
            <html>
              <body style='margin: 0;'>
                <div style='height: 860px;'>Block 1</div>
                <div style='height: 300px;'>Block 2</div>
              </body>
            </html>
            """,
            """
            boxes
            1:block path=body/div rect=0,0,612,645 content=0,0,612,645 marker=0 anonymous=false inlineBlock=false
            2:block path=body/div rect=0,645,612,225 content=0,645,612,225 marker=0 anonymous=false inlineBlock=false
            fragments
            page 1 size=612,792 margin=0,0,0,0
              1:block rect=0,0,612,645
                2:line rect=0,0,612,14.4 text="Block 1" occupied=0,0,10,14.4
            page 2 size=612,792 margin=0,0,0,0
              3:block rect=0,0,612,225
                4:line rect=0,0,612,14.4 text="Block 2" occupied=0,0,10,14.4
            pagination
            page 1 content=0..792
              placement order=0 fragment=1 kind=Block decision=Placed oversized=false rect=0,0,612,645
            page 2 content=0..792
              placement order=1 fragment=2 kind=Block decision=MovedToNextPage oversized=false rect=0,0,612,225
            """
        ];
    }

    private static IEnumerable<FragmentSnapshot> Flatten(
        IReadOnlyList<FragmentSnapshot> fragments)
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

    private static IEnumerable<BoxGeometrySnapshot> FlattenBoxes(
        BoxGeometrySnapshot box)
    {
        yield return box;

        foreach (var child in box.Children.SelectMany(FlattenBoxes))
        {
            yield return child;
        }
    }

    private static IEnumerable<Fragment> EnumerateFragments(Fragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nested in EnumerateFragments(child))
            {
                yield return nested;
            }
        }
    }

    private static bool ContainsText(BlockFragment fragment, string text)
    {
        return EnumerateFragments(fragment)
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Any(run => run.Text.Contains(text, StringComparison.Ordinal));
    }

    private static void AssertCommonTranslation(
        Fragment source,
        Fragment moved,
        float deltaX,
        float deltaY,
        int expectedPageNumber)
    {
        moved.FragmentId.ShouldBe(source.FragmentId);
        moved.PageNumber.ShouldBe(expectedPageNumber);
        moved.ZOrder.ShouldBe(source.ZOrder);
        moved.Style.ShouldBe(source.Style);
        moved.Rect.ShouldBe(new(
            source.Rect.X + deltaX,
            source.Rect.Y + deltaY,
            source.Rect.Width,
            source.Rect.Height));
    }
}