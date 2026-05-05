using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Test;

public sealed class BlockLayoutPublishedEquivalenceTests
{
    private readonly ITextMeasurer _textMeasurer = new FakeTextMeasurer(1f, 9f, 3f);

    public static IEnumerable<object[]> EquivalentLayoutCases()
    {
        yield return ["standard block", (Func<BlockBox>)CreateStandardRoot];
        yield return ["nested standard blocks", (Func<BlockBox>)CreateNestedStandardRoot];
        yield return ["image block", (Func<BlockBox>)CreateImageRoot];
        yield return ["rule block", (Func<BlockBox>)CreateRuleRoot];
        yield return ["inline text", (Func<BlockBox>)CreateInlineTextRoot];
        yield return ["inline-block content", (Func<BlockBox>)CreateInlineBlockRoot];
        yield return ["supported table", (Func<BlockBox>)CreateSupportedTableRoot];
        yield return ["unsupported table placeholder", (Func<BlockBox>)CreateUnsupportedTableRoot];
    }

    [Fact]
    public void LayoutImageBlock_PublishesImageFacts()
    {
        var image = CreateImageBlock();

        var published = CreateEngine().LayoutImageBlock(image, DefaultRequest());

        var facts = published.Image.ShouldNotBeNull();
        facts.Src.ShouldBe("images/logo.png");
        facts.AuthoredSizePx.ShouldBe(image.AuthoredSizePx);
        facts.IntrinsicSizePx.ShouldBe(image.IntrinsicSizePx);
        facts.IsMissing.ShouldBeFalse();
        facts.IsOversize.ShouldBeFalse();
        published.Geometry.ShouldBe(image.UsedGeometry.ShouldNotBeNull());
        published.Children.ShouldBeEmpty();
    }

    [Fact]
    public void LayoutImageBlock_AppliesImageState()
    {
        var image = CreateImageBlock();

        _ = CreateEngine().LayoutImageBlock(image, DefaultRequest());

        image.Src.ShouldBe("images/logo.png");
        image.AuthoredSizePx.Width.HasValue.ShouldBeTrue();
        image.AuthoredSizePx.Width.GetValueOrDefault().ShouldBeGreaterThan(0d);
        image.AuthoredSizePx.Height.HasValue.ShouldBeTrue();
        image.AuthoredSizePx.Height.GetValueOrDefault().ShouldBeGreaterThan(0d);
        image.UsedGeometry.ShouldNotBeNull().BorderBoxRect.Size.ShouldBe(new SizePt(30f, 12f));
    }

    [Fact]
    public void LayoutRuleBlock_PublishesRuleFacts()
    {
        var rule = CreateRuleBlock(new Spacing(1f, 0f, 1f, 0f));

        var published = CreateEngine().LayoutRuleBlock(rule, DefaultRequest());

        published.Rule.ShouldNotBeNull();
        published.Image.ShouldBeNull();
        published.Table.ShouldBeNull();
        published.Geometry.ShouldBe(rule.UsedGeometry.ShouldNotBeNull());
    }

    [Fact]
    public void LayoutRuleBlock_AppliesBoxGeometry()
    {
        var rule = CreateRuleBlock(new Spacing(2f, 0f, 3f, 0f));

        _ = CreateEngine().LayoutRuleBlock(rule, DefaultRequest());

        rule.UsedGeometry.ShouldNotBeNull();
        rule.UsedGeometry.Value.Height.ShouldBe(5f);
        rule.UsedGeometry.Value.Width.ShouldBe(160f);
    }

    [Fact]
    public void LayoutPublished_WithInlineText_PublishesInlineSegments()
    {
        var published = CreateEngine()
            .LayoutPublished(CreateInlineTextRoot(), DefaultPage())
            .Blocks
            .ShouldHaveSingleItem();

        var line = GetSingleInlineLine(published);
        var textItem = line.Items.ShouldHaveSingleItem().ShouldBeOfType<PublishedInlineTextItem>();

        textItem.Runs.ShouldHaveSingleItem().Text.ShouldBe("alpha");
        var source = textItem.Sources.ShouldHaveSingleItem();
        string.IsNullOrWhiteSpace(source.NodePath).ShouldBeFalse();
        source.SourceOrder.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void LayoutPublished_WithInlineText_PublishesInlineSourceIdentity()
    {
        var root = CreateRoot();
        var block = new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle()
        };
        var sourceIdentity = new GeometrySourceIdentity(
            new StyleNodeId(8),
            new StyleContentId(2),
            "body[0]/p[0]/text[0]",
            "p",
            15,
            GeometryGeneratedSourceKind.AnonymousText);
        block.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = block,
            Style = block.Style,
            TextContent = "alpha",
            SourceIdentity = sourceIdentity
        });
        root.Children.Add(block);

        var published = CreateEngine()
            .LayoutPublished(root, DefaultPage())
            .Blocks
            .ShouldHaveSingleItem();

        var source = GetSingleInlineLine(published)
            .Items
            .ShouldHaveSingleItem()
            .ShouldBeOfType<PublishedInlineTextItem>()
            .Sources
            .ShouldHaveSingleItem();

        source.SourceIdentity.ShouldBe(sourceIdentity);
        source.SourceIdentity.SourcePath.ShouldBe("body[0]/p[0]/text[0]");
    }

    [Fact]
    public void LayoutPublished_WithInlineBlock_PublishesInlineObjectContent()
    {
        var published = CreateEngine()
            .LayoutPublished(CreateInlineBlockRoot(), DefaultPage())
            .Blocks
            .ShouldHaveSingleItem();

        var objectItem = GetSingleInlineItems(published)
            .OfType<PublishedInlineObjectItem>()
            .ShouldHaveSingleItem();

        objectItem.Content.Display.FormattingContext.ShouldBe(FormattingContextKind.InlineBlock);
        objectItem.Content.InlineLayout.ShouldNotBeNull();
        objectItem.Content.Geometry.ShouldNotBe(default);
    }

    [Fact]
    public void LayoutPublished_WithInlineBlock_PreservesInlineFlowOrder()
    {
        var published = CreateEngine()
            .LayoutPublished(CreateInlineBlockRoot(), DefaultPage())
            .Blocks
            .ShouldHaveSingleItem();

        var items = GetSingleInlineItems(published);

        items.Count.ShouldBe(3);
        items[0].ShouldBeOfType<PublishedInlineTextItem>().Runs.ShouldHaveSingleItem().Text.ShouldBe("prefix ");
        items[1].ShouldBeOfType<PublishedInlineObjectItem>();
        items[2].ShouldBeOfType<PublishedInlineTextItem>().Runs.ShouldHaveSingleItem().Text.ShouldBe(" suffix");
    }

    [Fact]
    public void LayoutTableBlock_PublishesTableFacts()
    {
        var table = CreateSupportedTable();

        var published = CreateEngine().LayoutTableBlock(table, DefaultRequest());

        published.Table.ShouldNotBeNull().DerivedColumnCount.ShouldBe(2);
        var row = published.Children.ShouldHaveSingleItem();
        row.Table.ShouldNotBeNull().RowIndex.ShouldBe(0);
        row.Children.Count.ShouldBe(2);
        row.Children[0].Table.ShouldNotBeNull().ColumnIndex.ShouldBe(0);
        row.Children[0].Table!.IsHeader.ShouldBe(true);
        row.Children[1].Table.ShouldNotBeNull().ColumnIndex.ShouldBe(1);
        row.Children[1].Table!.IsHeader.ShouldBe(false);
    }

    [Fact]
    public void LayoutTableBlock_WithUnsupportedStructure_PublishesPlaceholderGeometry()
    {
        var table = CreateUnsupportedTable();

        var published = CreateEngine().LayoutTableBlock(table, DefaultRequest());

        published.Table.ShouldNotBeNull().DerivedColumnCount.ShouldBe(0);
        published.Geometry.Height.ShouldBe(0f);
        published.Children.ShouldBeEmpty();
        table.Children.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(EquivalentLayoutCases))]
    public void LayoutPublished_WithCompletedBehavior_ProducesRendererVisibleFragments(
        string caseName,
        object createRootFactory)
    {
        _ = caseName;
        var createRoot = createRootFactory.ShouldBeOfType<Func<BlockBox>>();
        var published = CreateEngine().LayoutPublished(createRoot(), DefaultPage());
        var fragments = new FragmentBuilder().Build(published);

        AssertPublishedFragments(published, fragments);
    }

    private BlockLayoutEngine CreateEngine()
    {
        var imageResolver = new ImageLayoutResolver();
        var inlineEngine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            _textMeasurer,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            imageResolver);

        return new BlockLayoutEngine(
            inlineEngine,
            new TableLayoutEngine(inlineEngine, imageResolver));
    }

    private static PageBox DefaultPage() => new()
    {
        Margin = new Spacing(0f, 0f, 0f, 0f),
        Size = new SizePt(200f, 400f)
    };

    private static BlockLayoutRequest DefaultRequest() => new(
        ContentX: 0f,
        CursorY: 0f,
        ContentWidth: 160f,
        ParentContentTop: 0f,
        PreviousBottomMargin: 0f,
        CollapsedTopMargin: 0f);

    private static ImageBox CreateImageBlock(
        float widthPt = 30f,
        float heightPt = 12f,
        string src = "images/logo.png",
        BlockBox? parent = null)
    {
        return new ImageBox(BoxRole.Block)
        {
            Parent = parent,
            Src = src,
            Style = new ComputedStyle
            {
                WidthPt = widthPt,
                HeightPt = heightPt
            }
        };
    }

    private static RuleBox CreateRuleBlock(Spacing padding, BlockBox? parent = null)
    {
        return new RuleBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new ComputedStyle
            {
                Padding = padding
            }
        };
    }

    private static BlockBox CreateStandardRoot()
    {
        var root = CreateRoot();
        root.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle
            {
                HeightPt = 20f
            }
        });

        return root;
    }

    private static BlockBox CreateNestedStandardRoot()
    {
        var root = CreateRoot();
        var parent = new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle()
        };

        parent.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new ComputedStyle
            {
                HeightPt = 10f
            }
        });
        parent.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new ComputedStyle
            {
                HeightPt = 12f
            }
        });
        root.Children.Add(parent);

        return root;
    }

    private static BlockBox CreateImageRoot()
    {
        var root = CreateRoot();
        root.Children.Add(CreateImageBlock(widthPt: 24f, heightPt: 12f, parent: root));

        return root;
    }

    private static BlockBox CreateRuleRoot()
    {
        var root = CreateRoot();
        root.Children.Add(CreateRuleBlock(new Spacing(1f, 0f, 1f, 0f), root));

        return root;
    }

    private static BlockBox CreateInlineTextRoot()
    {
        var root = CreateRoot();
        var block = new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle()
        };
        block.Children.Add(CreateInline(block, "alpha"));
        root.Children.Add(block);

        return root;
    }

    private static BlockBox CreateInlineBlockRoot()
    {
        var root = CreateRoot();
        var block = new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle()
        };
        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Parent = block,
            Element = CreateElement("span"),
            Style = new ComputedStyle
            {
                Display = HtmlCssConstants.CssValues.InlineBlock,
                Padding = new Spacing(1f, 1f, 1f, 1f)
            }
        };
        var content = new BlockBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            Element = inlineBlock.Element,
            IsInlineBlockContext = true,
            Style = inlineBlock.Style
        };
        content.Children.Add(CreateInline(content, "inside"));
        inlineBlock.Children.Add(content);

        block.Children.Add(CreateInline(block, "prefix "));
        block.Children.Add(inlineBlock);
        block.Children.Add(CreateInline(block, " suffix"));
        root.Children.Add(block);

        return root;
    }

    private static BlockBox CreateSupportedTableRoot()
    {
        var root = CreateRoot();
        var table = CreateSupportedTable(root);
        root.Children.Add(table);

        return root;
    }

    private static BlockBox CreateUnsupportedTableRoot()
    {
        var root = CreateRoot();
        var table = CreateUnsupportedTable(root);
        root.Children.Add(table);

        return root;
    }

    private static TableBox CreateSupportedTable(BlockBox? parent = null)
    {
        var table = new TableBox(BoxRole.Table)
        {
            Parent = parent,
            Element = CreateElement("table"),
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            Element = CreateElement("tr"),
            Style = new ComputedStyle()
        };
        var header = CreateTableCell(row, "th", "head");
        var cell = CreateTableCell(row, "td", "body");

        row.Children.Add(header);
        row.Children.Add(cell);
        table.Children.Add(row);

        return table;
    }

    private static TableBox CreateUnsupportedTable(BlockBox? parent = null)
    {
        var table = new TableBox(BoxRole.Table)
        {
            Parent = parent,
            Element = CreateElement("table"),
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            Element = CreateElement("tr"),
            Style = new ComputedStyle()
        };
        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            Element = CreateElement("td", (HtmlCssConstants.HtmlAttributes.Colspan, "2")),
            Style = new ComputedStyle()
        };

        row.Children.Add(cell);
        table.Children.Add(row);

        return table;
    }

    private static TableCellBox CreateTableCell(TableRowBox row, string tagName, string text)
    {
        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            Element = CreateElement(tagName),
            Style = new ComputedStyle()
        };
        cell.Children.Add(CreateInline(cell, text));

        return cell;
    }

    private static BlockBox CreateRoot()
    {
        return new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
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

    private static StyledElementFacts CreateElement(
        string tagName,
        params (string Name, string Value)[] attributes)
    {
        return StyledElementFacts.Create(tagName, attributes);
    }

    private static PublishedInlineLine GetSingleInlineLine(PublishedBlock block)
    {
        return block.InlineLayout
            .ShouldNotBeNull()
            .Segments
            .ShouldHaveSingleItem()
            .Lines
            .ShouldHaveSingleItem();
    }

    private static IReadOnlyList<PublishedInlineItem> GetSingleInlineItems(PublishedBlock block)
    {
        return GetSingleInlineLine(block).Items;
    }

    private static void AssertBlocksEquivalent(
        IReadOnlyList<PublishedBlock> actual,
        IReadOnlyList<PublishedBlock> expected)
    {
        actual.Count.ShouldBe(expected.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            AssertBlockEquivalent(actual[i], expected[i]);
        }
    }

    private static void AssertBlockEquivalent(PublishedBlock actual, PublishedBlock expected)
    {
        actual.Identity.ShouldBe(expected.Identity);
        actual.Display.ShouldBe(expected.Display);
        actual.Style.ShouldBe(expected.Style);
        actual.Geometry.ShouldBe(expected.Geometry);
        actual.Image.ShouldBe(expected.Image);
        actual.Rule.ShouldBe(expected.Rule);
        actual.Table.ShouldBe(expected.Table);
        AssertInlineLayoutEquivalent(actual.InlineLayout, expected.InlineLayout);
        AssertBlocksEquivalent(actual.Children, expected.Children);
    }

    private static void AssertInlineLayoutEquivalent(
        PublishedInlineLayout? actual,
        PublishedInlineLayout? expected)
    {
        if (expected is null)
        {
            actual.ShouldBeNull();
            return;
        }

        actual.ShouldNotBeNull();
        actual.TotalHeight.ShouldBe(expected.TotalHeight);
        actual.MaxLineWidth.ShouldBe(expected.MaxLineWidth);
        actual.Segments.Count.ShouldBe(expected.Segments.Count);

        for (var i = 0; i < expected.Segments.Count; i++)
        {
            AssertInlineSegmentEquivalent(actual.Segments[i], expected.Segments[i]);
        }
    }

    private static void AssertInlineSegmentEquivalent(
        PublishedInlineFlowSegment actual,
        PublishedInlineFlowSegment expected)
    {
        actual.Top.ShouldBe(expected.Top);
        actual.Height.ShouldBe(expected.Height);
        actual.Lines.Count.ShouldBe(expected.Lines.Count);

        for (var i = 0; i < expected.Lines.Count; i++)
        {
            AssertInlineLineEquivalent(actual.Lines[i], expected.Lines[i]);
        }
    }

    private static void AssertInlineLineEquivalent(
        PublishedInlineLine actual,
        PublishedInlineLine expected)
    {
        actual.LineIndex.ShouldBe(expected.LineIndex);
        actual.Rect.ShouldBe(expected.Rect);
        actual.OccupiedRect.ShouldBe(expected.OccupiedRect);
        actual.BaselineY.ShouldBe(expected.BaselineY);
        actual.LineHeight.ShouldBe(expected.LineHeight);
        actual.TextAlign.ShouldBe(expected.TextAlign);
        actual.Items.Count.ShouldBe(expected.Items.Count);

        for (var i = 0; i < expected.Items.Count; i++)
        {
            AssertInlineItemEquivalent(actual.Items[i], expected.Items[i]);
        }
    }

    private static void AssertInlineItemEquivalent(
        PublishedInlineItem actual,
        PublishedInlineItem expected)
    {
        actual.Order.ShouldBe(expected.Order);
        actual.Rect.ShouldBe(expected.Rect);
        actual.GetType().ShouldBe(expected.GetType());

        switch (expected)
        {
            case PublishedInlineTextItem expectedText:
                var actualText = actual.ShouldBeOfType<PublishedInlineTextItem>();
                actualText.Runs.ShouldBe(expectedText.Runs);
                actualText.Sources.ShouldBe(expectedText.Sources);
                break;
            case PublishedInlineObjectItem expectedObject:
                var actualObject = actual.ShouldBeOfType<PublishedInlineObjectItem>();
                AssertBlockEquivalent(actualObject.Content, expectedObject.Content);
                break;
        }
    }

    private static void AssertFragmentsEquivalent(
        IReadOnlyList<BlockFragment> actual,
        IReadOnlyList<BlockFragment> expected)
    {
        actual.Count.ShouldBe(expected.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            AssertFragmentEquivalent(actual[i], expected[i]);
        }
    }

    private static void AssertPublishedFragments(PublishedLayoutTree published, FragmentTree fragments)
    {
        fragments.Blocks.Count.ShouldBe(published.Blocks.Count);

        for (var i = 0; i < published.Blocks.Count; i++)
        {
            AssertPublishedFragment(published.Blocks[i], fragments.Blocks[i]);
        }
    }

    private static void AssertPublishedFragment(PublishedBlock published, BlockFragment fragment)
    {
        fragment.Rect.ShouldBe(published.Geometry.BorderBoxRect);
        fragment.DisplayRole.ShouldBe(published.Display.Role);
        fragment.FormattingContext.ShouldBe(published.Display.FormattingContext);
        fragment.MarkerOffset.ShouldBe(published.Display.MarkerOffset);

        if (published.Image is not null)
        {
            fragment.Children.OfType<ImageFragment>().ShouldHaveSingleItem();
        }

        if (published.Rule is not null)
        {
            fragment.Children.OfType<RuleFragment>().ShouldHaveSingleItem();
        }

        var publishedChildren = published.Flow
            .OrderBy(static item => item.Order)
            .OfType<PublishedChildBlockItem>()
            .Select(static item => item.Block)
            .ToList();
        var fragmentChildren = fragment.Children.OfType<BlockFragment>().ToList();

        for (var i = 0; i < publishedChildren.Count; i++)
        {
            AssertPublishedFragment(publishedChildren[i], fragmentChildren[i]);
        }
    }

    private static void AssertFragmentEquivalent(
        Fragment actual,
        Fragment expected)
    {
        actual.GetType().ShouldBe(expected.GetType());
        actual.PageNumber.ShouldBe(expected.PageNumber);
        actual.Rect.ShouldBe(expected.Rect);
        actual.Style.ShouldBe(expected.Style);

        switch (actual, expected)
        {
            case (TableFragment actualTable, TableFragment expectedTable):
                actualTable.DerivedColumnCount.ShouldBe(expectedTable.DerivedColumnCount);
                AssertFragmentsEquivalent(
                    actualTable.Rows.Cast<BlockFragment>().ToList(),
                    expectedTable.Rows.Cast<BlockFragment>().ToList());
                break;
            case (TableRowFragment actualRow, TableRowFragment expectedRow):
                actualRow.RowIndex.ShouldBe(expectedRow.RowIndex);
                AssertFragmentsEquivalent(
                    actualRow.Cells.Cast<BlockFragment>().ToList(),
                    expectedRow.Cells.Cast<BlockFragment>().ToList());
                break;
            case (TableCellFragment actualCell, TableCellFragment expectedCell):
                actualCell.ColumnIndex.ShouldBe(expectedCell.ColumnIndex);
                actualCell.IsHeader.ShouldBe(expectedCell.IsHeader);
                AssertFragmentChildrenEquivalent(actualCell, expectedCell);
                break;
            case (BlockFragment actualBlock, BlockFragment expectedBlock):
                actualBlock.DisplayRole.ShouldBe(expectedBlock.DisplayRole);
                actualBlock.FormattingContext.ShouldBe(expectedBlock.FormattingContext);
                actualBlock.MarkerOffset.ShouldBe(expectedBlock.MarkerOffset);
                AssertFragmentChildrenEquivalent(actualBlock, expectedBlock);
                break;
            case (LineBoxFragment actualLine, LineBoxFragment expectedLine):
                actualLine.OccupiedRect.ShouldBe(expectedLine.OccupiedRect);
                actualLine.BaselineY.ShouldBe(expectedLine.BaselineY);
                actualLine.LineHeight.ShouldBe(expectedLine.LineHeight);
                actualLine.TextAlign.ShouldBe(expectedLine.TextAlign);
                actualLine.Runs.ShouldBe(expectedLine.Runs);
                break;
            case (ImageFragment actualImage, ImageFragment expectedImage):
                actualImage.Src.ShouldBe(expectedImage.Src);
                actualImage.AuthoredSizePx.ShouldBe(expectedImage.AuthoredSizePx);
                actualImage.IntrinsicSizePx.ShouldBe(expectedImage.IntrinsicSizePx);
                actualImage.IsMissing.ShouldBe(expectedImage.IsMissing);
                actualImage.IsOversize.ShouldBe(expectedImage.IsOversize);
                actualImage.ContentRect.ShouldBe(expectedImage.ContentRect);
                break;
        }
    }

    private static void AssertFragmentChildrenEquivalent(BlockFragment actual, BlockFragment expected)
    {
        actual.Children.Count.ShouldBe(expected.Children.Count);
        for (var i = 0; i < expected.Children.Count; i++)
        {
            AssertFragmentEquivalent(actual.Children[i], expected.Children[i]);
        }
    }
}
