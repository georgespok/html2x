using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class BlockContentMeasurementServiceTests
{
    private readonly ITextMeasurer _textMeasurer = new FakeTextMeasurer(10f, 9f, 3f);

    public static IEnumerable<object[]> MeasurementSensitiveBlockCases()
    {
        yield return ["inline text", (Func<BlockBox>)(() => CreateInlineTextBlock())];
        yield return ["nested block", (Func<BlockBox>)CreateNestedBlockContent];
        yield return ["nested table", (Func<BlockBox>)CreateNestedTableContent];
        yield return ["inline-block with inline text", (Func<BlockBox>)CreateInlineBlockContent];
        yield return ["inline-block with block descendants", (Func<BlockBox>)CreateInlineBlockWithBlockDescendantsContent];
        yield return ["image child", (Func<BlockBox>)CreateImageContent];
        yield return ["rule child", (Func<BlockBox>)CreateRuleContent];
    }

    [Fact]
    public void Measure_WithInlineText_DoesNotAssignInlineLayout()
    {
        var block = CreateInlineTextBlock();
        var existingLayout = CreateInlineLayout();
        block.InlineLayout = existingLayout;

        var measurement = CreateMeasurementService().Measure(block, 40f, MeasureNoTables);

        measurement.BorderBoxHeight.ShouldBeGreaterThan(0f);
        measurement.InlineHeight.ShouldBeGreaterThan(0f);
        measurement.NestedBlockHeight.ShouldBe(0f);
        block.InlineLayout.ShouldBeSameAs(existingLayout);
    }

    [Fact]
    public void Measure_Image_ReturnsFactsWithoutMutatingInputs()
    {
        var image = new ImageBox(BoxRole.Block)
        {
            Src = "before.png",
            AuthoredSizePx = new SizePx(1d, 2d),
            IntrinsicSizePx = new SizePx(3d, 4d),
            IsMissing = true,
            IsOversize = true,
            Style = new ComputedStyle
            {
                WidthPt = 30f,
                HeightPt = 15f
            }
        };
        var originalGeometry = CreateGeometry();
        image.ApplyLayoutGeometry(originalGeometry);

        var measurement = CreateMeasurementService(new FixedImageMetadataResolver(new SizePx(40d, 20d)))
            .Measure(image, 100f, MeasureNoTables);

        var imageFacts = measurement.Image.ShouldNotBeNull();
        imageFacts.Src.ShouldBe("before.png");
        imageFacts.IntrinsicSizePx.ShouldBe(new SizePx(40d, 20d));
        measurement.BorderBoxHeight.ShouldBeGreaterThan(0f);
        image.UsedGeometry.ShouldBe(originalGeometry);
        image.Src.ShouldBe("before.png");
        image.AuthoredSizePx.ShouldBe(new SizePx(1d, 2d));
        image.IntrinsicSizePx.ShouldBe(new SizePx(3d, 4d));
        image.IsMissing.ShouldBeTrue();
        image.IsOversize.ShouldBeTrue();
    }

    [Fact]
    public void Measure_WithRule_ReturnsStyleHeightWithoutGeometryMutation()
    {
        var rule = new RuleBox(BoxRole.Block)
        {
            Style = new ComputedStyle
            {
                Padding = new Spacing(2f, 0f, 3f, 0f),
                Borders = BorderEdges.Uniform(new BorderSide(1f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        var originalGeometry = CreateGeometry();
        rule.ApplyLayoutGeometry(originalGeometry);

        var measurement = CreateMeasurementService().Measure(rule, 100f, MeasureNoTables);

        measurement.BorderBoxHeight.ShouldBe(7f);
        measurement.ContentHeight.ShouldBe(0f);
        rule.UsedGeometry.ShouldBe(originalGeometry);
        rule.InlineLayout.ShouldBeNull();
    }

    [Fact]
    public void Measure_WithSupportedTable_ReturnsTableFactsWithoutRowOrCellMutation()
    {
        var table = CreateSupportedTable("table cell text that wraps across several lines", widthPt: 60f);
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        var measurement = CreateMeasurementService().Measure(table, 80f, MeasureTable);

        measurement.Table.ShouldNotBeNull();
        measurement.BorderBoxHeight.ShouldBeGreaterThan(0f);
        table.UsedGeometry.ShouldBeNull();
        table.DerivedColumnCount.ShouldBe(-1);
        row.UsedGeometry.ShouldBeNull();
        row.RowIndex.ShouldBe(-1);
        cell.UsedGeometry.ShouldBeNull();
        cell.ColumnIndex.ShouldBe(-1);
        cell.IsHeader.ShouldBeFalse();
    }

    [Fact]
    public void Measure_WithNestedInlineBlock_DoesNotAssignInlineObjectGeometry()
    {
        var block = CreateInlineBlockContent();
        var inlineBlock = block.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();
        var content = inlineBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        var existingLayout = CreateInlineLayout();
        content.InlineLayout = existingLayout;

        var measurement = CreateMeasurementService().Measure(block, 120f, MeasureNoTables);

        measurement.BorderBoxHeight.ShouldBeGreaterThan(0f);
        content.UsedGeometry.ShouldBeNull();
        content.InlineLayout.ShouldBeSameAs(existingLayout);
        inlineBlock.Width.ShouldBe(0f);
        inlineBlock.Height.ShouldBe(0f);
    }

    [Fact]
    public void Measure_WithRepeatedWidths_ReturnsDeterministicResults()
    {
        var block = CreateInlineTextBlock("abcd efgh ijkl mnop qrst");
        var existingLayout = CreateInlineLayout();
        block.InlineLayout = existingLayout;
        var measurement = CreateMeasurementService();

        var wide = measurement.Measure(block, 200f, MeasureNoTables);
        var firstNarrow = measurement.Measure(block, 35f, MeasureNoTables);
        var secondNarrow = measurement.Measure(block, 35f, MeasureNoTables);

        firstNarrow.BorderBoxHeight.ShouldBeGreaterThan(wide.BorderBoxHeight);
        secondNarrow.ShouldBe(firstNarrow);
        block.InlineLayout.ShouldBeSameAs(existingLayout);
    }

    [Fact]
    public void Measure_WithTableCellInlineText_MatchesTableRowHeight()
    {
        var table = CreateSupportedTable(
            "table cell text that wraps across many short measured lines",
            widthPt: 50f);
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        var tableResult = CreateTableEngine().Layout(table, 100f);
        var rowResult = tableResult.Rows.ShouldHaveSingleItem();
        var assignedWidth = rowResult.Cells.ShouldHaveSingleItem().UsedGeometry.Width;
        var measuredCell = CreateMeasurementService().Measure(cell, assignedWidth, MeasureTable);

        rowResult.UsedGeometry.Height.ShouldBeGreaterThan(20f);
        measuredCell.BorderBoxHeight.ShouldBe(rowResult.UsedGeometry.Height, 0.01f);
    }

    [Theory]
    [MemberData(nameof(MeasurementSensitiveBlockCases))]
    public void Measure_WithMeasurementSensitiveBlock_MatchesStandardLayoutHeight(
        string caseName,
        object createBlockFactory)
    {
        _ = caseName;
        var createBlock = createBlockFactory.ShouldBeOfType<Func<BlockBox>>();
        var measured = CreateMeasurementService().Measure(createBlock(), 120f, MeasureTable);

        var layoutBlock = createBlock();
        var published = CreateBlockLayoutEngine().LayoutStandardBlock(
            layoutBlock,
            new BlockLayoutRequest(
                ContentX: 0f,
                CursorY: 0f,
                ContentWidth: 120f,
                ParentContentTop: 0f,
                PreviousBottomMargin: 0f,
                CollapsedTopMargin: 0f));

        measured.BorderBoxHeight.ShouldBe(published.Geometry.Height, 0.01f);
    }

    private BlockContentMeasurementService CreateMeasurementService(IImageMetadataResolver? imageMetadataResolver = null)
    {
        var imageResolver = CreateImageResolver(imageMetadataResolver);
        return new BlockContentMeasurementService(
            CreateInlineEngine(imageResolver),
            new BlockMeasurementService(),
            imageResolver);
    }

    private BlockLayoutEngine CreateBlockLayoutEngine(IImageMetadataResolver? imageMetadataResolver = null)
    {
        var imageResolver = CreateImageResolver(imageMetadataResolver);
        var inlineEngine = CreateInlineEngine(imageResolver);
        return new BlockLayoutEngine(
            inlineEngine,
            new TableLayoutEngine(inlineEngine, imageResolver));
    }

    private TableLayoutEngine CreateTableEngine(IImageMetadataResolver? imageMetadataResolver = null)
    {
        var imageResolver = CreateImageResolver(imageMetadataResolver);
        return new TableLayoutEngine(CreateInlineEngine(imageResolver), imageResolver);
    }

    private InlineLayoutEngine CreateInlineEngine(IImageLayoutResolver imageResolver)
    {
        return new InlineLayoutEngine(
            new FontMetricsProvider(),
            _textMeasurer,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            imageResolver);
    }

    private static ImageLayoutResolver CreateImageResolver(IImageMetadataResolver? imageMetadataResolver = null)
    {
        return new ImageLayoutResolver(imageMetadataResolver is null
            ? null
            : new LayoutGeometryRequest
            {
                ImageMetadataResolver = imageMetadataResolver
            });
    }

    private BlockContentMeasurement MeasureTable(TableBox table, float availableWidth)
    {
        return BlockContentMeasurement.ForTable(CreateTableEngine().Layout(table, availableWidth));
    }

    private static BlockContentMeasurement MeasureNoTables(TableBox table, float availableWidth)
    {
        return BlockContentMeasurement.ForBorderBoxHeight(0f);
    }

    private static BlockBox CreateInlineTextBlock(string text = "alpha beta gamma")
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        block.Children.Add(CreateInline(block, text));
        return block;
    }

    private static BlockBox CreateNestedBlockContent()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        block.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = block,
            Style = new ComputedStyle
            {
                HeightPt = 18f,
                Margin = new Spacing(0f, 0f, 4f, 0f)
            }
        });
        block.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = block,
            Style = new ComputedStyle
            {
                HeightPt = 12f,
                Margin = new Spacing(3f, 0f, 0f, 0f)
            }
        });
        return block;
    }

    private static BlockBox CreateNestedTableContent()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        var table = CreateSupportedTable("nested table text that wraps", widthPt: 80f, parent: block);
        block.Children.Add(table);
        return block;
    }

    private static BlockBox CreateInlineBlockContent()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        var inlineBlock = CreateInlineBlock(block);
        var content = inlineBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        content.Children.Add(CreateInline(content, "inside inline block"));
        block.Children.Add(inlineBlock);
        return block;
    }

    private static BlockBox CreateInlineBlockWithBlockDescendantsContent()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        var inlineBlock = CreateInlineBlock(block);
        var content = inlineBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        content.Children.Add(CreateInline(content, "before"));
        var childBlock = new BlockBox(BoxRole.Block)
        {
            Parent = content,
            Style = new ComputedStyle
            {
                HeightPt = 14f
            }
        };
        childBlock.Children.Add(CreateInline(childBlock, "block child"));
        content.Children.Add(childBlock);
        content.Children.Add(CreateInline(content, "after"));
        block.Children.Add(inlineBlock);
        return block;
    }

    private static BlockBox CreateImageContent()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        block.Children.Add(new ImageBox(BoxRole.Block)
        {
            Parent = block,
            Src = "image.png",
            Style = new ComputedStyle
            {
                WidthPt = 24f,
                HeightPt = 12f
            }
        });
        return block;
    }

    private static BlockBox CreateRuleContent()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        block.Children.Add(new RuleBox(BoxRole.Block)
        {
            Parent = block,
            Style = new ComputedStyle
            {
                Padding = new Spacing(2f, 0f, 3f, 0f)
            }
        });
        return block;
    }

    private static InlineBox CreateInlineBlock(BlockBox parent)
    {
        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Parent = parent,
            Style = new ComputedStyle
            {
                Display = HtmlCssConstants.CssValues.InlineBlock,
                Padding = new Spacing(1f, 1f, 1f, 1f)
            }
        };
        inlineBlock.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            Style = inlineBlock.Style,
            IsInlineBlockContext = true
        });
        return inlineBlock;
    }

    private static TableBox CreateSupportedTable(
        string text,
        float widthPt,
        BlockBox? parent = null)
    {
        var table = new TableBox(BoxRole.Table)
        {
            Parent = parent,
            Style = new ComputedStyle
            {
                WidthPt = widthPt
            }
        };
        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            Style = new ComputedStyle()
        };
        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            Style = new ComputedStyle()
        };
        cell.Children.Add(CreateInline(cell, text));
        row.Children.Add(cell);
        table.Children.Add(row);
        return table;
    }

    private static InlineBox CreateInline(BlockBox parent, string text)
    {
        return new InlineBox(BoxRole.Inline)
        {
            Parent = parent,
            Style = parent.Style,
            TextContent = text
        };
    }

    private static InlineLayoutResult CreateInlineLayout()
    {
        return new InlineLayoutResult(
            [
                new InlineFlowSegmentLayout([], Top: 123f, Height: 45f)
            ],
            TotalHeight: 45f,
            MaxLineWidth: 67f);
    }

    private static UsedGeometry CreateGeometry()
    {
        return BoxGeometryFactory.FromBorderBox(
            1f,
            2f,
            30f,
            10f,
            new Spacing(),
            new Spacing());
    }

    private sealed class FixedImageMetadataResolver(SizePx intrinsicSize) : IImageMetadataResolver
    {
        public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes)
        {
            return new ImageMetadataResult
            {
                Src = src,
                Status = ImageMetadataStatus.Ok,
                IntrinsicSizePx = intrinsicSize
            };
        }
    }
}
