using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test;

public class BlockBoxLayoutTests
{
    private readonly ITextMeasurer _textMeasurer = new FakeTextMeasurer(1f, 9f, 3f);

    private static PageBox DefaultPage() => new()
    {
        Margin = new(0, 0, 0, 0),
        Size = new(200, 400)
    };

    [Fact]
    public void Layout_BlockHeightIncludesPadding()
    {
        var root = new BlockBoxBuilder()
            .Block(0, 0, 0, 0, style: new())
            .WithPadding(10f, bottom: 6f)
            .Inline("content")
            .BuildRoot();


        // Act
        var result = LayoutMutableBlocks(root);

        // Assert
        var block = result.ShouldHaveSingleItem();
        block.InlineLayout.ShouldNotBeNull();
        block.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(block.InlineLayout!.TotalHeight + 10f + 6f, 0.01f);
    }

    [Fact]
    public void Layout_Block_PopulatesUsedGeometry()
    {
        var style = new ComputedStyle
        {
            Padding = new(4f, 5f, 6f, 7f),
            Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
        };

        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        root.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = style,
            MarkerOffset = 9f
        });

        var result = LayoutMutableBlocks(root);

        var block = result.ShouldHaveSingleItem();
        block.UsedGeometry.ShouldNotBeNull();

        var geometry = block.UsedGeometry!.Value;
        geometry.BorderBoxRect.X.ShouldBe(0f);
        geometry.BorderBoxRect.Y.ShouldBe(0f);
        geometry.BorderBoxRect.Width.ShouldBe(200f);
        geometry.ContentBoxRect.X.ShouldBe(geometry.BorderBoxRect.X + 9f);
        geometry.ContentBoxRect.Y.ShouldBe(geometry.BorderBoxRect.Y + 6f);
        geometry.ContentBoxRect.Width.ShouldBe(geometry.BorderBoxRect.Width - 16f);
        geometry.ContentBoxRect.Height.ShouldBe(geometry.BorderBoxRect.Height - 14f);
        geometry.MarkerOffset.ShouldBe(9f);
    }

    [Fact]
    public void LayoutStandardBlock_PublishesGeometryAndDisplayFacts()
    {
        var block = new BlockBox(BoxRole.ListItem)
        {
            Style = new()
            {
                HeightPt = 20f
            },
            MarkerOffset = 6f
        };

        var published = CreateBlockBoxLayout().LayoutStandardBlock(
            block,
            new(
                10f,
                15f,
                120f,
                15f,
                0f,
                0f));

        published.Identity.NodePath.ShouldBe("listitem");
        published.Identity.SourceOrder.ShouldBe(0);
        published.Display.Role.ShouldBe(FragmentDisplayRole.ListItem);
        published.Display.FormattingContext.ShouldBe(FormattingContextKind.Block);
        published.Display.MarkerOffset.ShouldBe(6f);
        published.Geometry.ShouldBe(block.UsedGeometry.ShouldNotBeNull());
        published.Geometry.BorderBoxRect.ShouldBe(block.UsedGeometry.Value.BorderBoxRect);
        published.Geometry.Height.ShouldBe(20f);
        published.InlineLayout.ShouldNotBeNull().Segments.ShouldBeEmpty();
        published.Children.ShouldBeEmpty();
    }

    [Fact]
    public void LayoutStandardBlock_AppliesBoxGeometry()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new()
            {
                Padding = new(2f, 3f, 4f, 5f),
                HeightPt = 12f
            }
        };

        _ = CreateBlockBoxLayout().LayoutStandardBlock(
            block,
            new(
                8f,
                9f,
                100f,
                9f,
                0f,
                0f));

        var geometry = block.UsedGeometry.ShouldNotBeNull();
        geometry.X.ShouldBe(8f);
        geometry.Y.ShouldBe(9f);
        geometry.Height.ShouldBe(18f);
        geometry.BorderBoxRect.ShouldBe(new(8f, 9f, 100f, 18f));
        geometry.ContentBoxRect.Y.ShouldBe(geometry.Y + 2f);
        geometry.ContentBoxRect.Height.ShouldBe(12f);
    }

    [Fact]
    public void ResolvePublished_WithStandardBlock_PublishesPageAndBlockFacts()
    {
        var page = DefaultPage();
        page.Margin = new(1f, 2f, 3f, 4f);
        var root = CreatePublishedSeamRoot();

        var published = ResolvePublished(root, page);

        published.Page.Size.ShouldBe(page.Size);
        published.Page.Margin.ShouldBe(page.Margin);
        var block = published.Blocks.ShouldHaveSingleItem();
        block.Identity.NodePath.ShouldBe("block/block");
        block.Display.Role.ShouldBe(FragmentDisplayRole.Block);
        block.Geometry.Height.ShouldBe(20f);
        root.Children.ShouldHaveSingleItem()
            .ShouldBeOfType<BlockBox>()
            .UsedGeometry
            .ShouldNotBeNull();
    }

    [Fact]
    public void ResolvePublished_WithNestedStandardBlocks_PublishesChildrenInOrder()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        var parent = new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new()
        };
        var first = new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new() { HeightPt = 10f }
        };
        var second = new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new() { HeightPt = 12f }
        };
        parent.Children.Add(first);
        parent.Children.Add(second);
        root.Children.Add(parent);

        var published = ResolvePublished(root, DefaultPage())
            .Blocks
            .ShouldHaveSingleItem();

        published.Children.Count.ShouldBe(2);
        published.Children[0].Geometry.Height.ShouldBe(10f);
        published.Children[1].Geometry.Height.ShouldBe(12f);
        published.Children[0].Identity.SourceOrder.ShouldBeLessThan(published.Children[1].Identity.SourceOrder);
    }

    [Fact]
    public void Layout_BlockContentGeometry_AccountsForPaddingBorderAndMarkerOffset()
    {
        var style = new ComputedStyle
        {
            Padding = new(3f, 4f, 5f, 7f),
            Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
        };

        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        var listItem = new BlockBox(BoxRole.ListItem)
        {
            Parent = root,
            Style = style,
            MarkerOffset = 11f
        };
        listItem.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = listItem,
            Style = new(),
            TextContent = "content"
        });
        root.Children.Add(listItem);

        var result = LayoutMutableBlocks(root);

        var block = result.ShouldHaveSingleItem();
        var geometry = block.UsedGeometry.ShouldNotBeNull();
        geometry.ContentBoxRect.ShouldBe(new(
            geometry.BorderBoxRect.X + 9f,
            geometry.BorderBoxRect.Y + 5f,
            geometry.BorderBoxRect.Width - 15f,
            geometry.BorderBoxRect.Height - 12f));
        geometry.MarkerOffset.ShouldBe(11f);
        block.InlineLayout.ShouldNotBeNull();
        block.InlineLayout!.Segments[0].Lines[0].Rect.X.ShouldBe(geometry.ContentBoxRect.X + geometry.MarkerOffset);
    }

    [Fact]
    public void Layout_BlockHeightPolicy_AppliesExplicitHeightAfterFlowHeights()
    {
        var parent = new BlockBox(BoxRole.Block)
        {
            Style = new()
            {
                HeightPt = 12f
            }
        };
        parent.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new()
            {
                HeightPt = 30f
            }
        });
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        root.Children.Add(parent);

        var result = LayoutMutableBlocks(root);

        var laidOutParent = result.ShouldHaveSingleItem();
        laidOutParent.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(12f);
        laidOutParent.Children.ShouldHaveSingleItem()
            .ShouldBeOfType<BlockBox>()
            .UsedGeometry
            .ShouldNotBeNull()
            .Height
            .ShouldBe(30f);
    }

    [Fact]
    public void Layout_ListItemMarkerOffset_ShiftsInlineContentOrigin()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        var listItem = new BlockBox(BoxRole.ListItem)
        {
            Parent = root,
            Style = new(),
            MarkerOffset = 12f
        };
        listItem.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = listItem,
            Style = new(),
            TextContent = "content"
        });
        root.Children.Add(listItem);

        _ = LayoutMutableBlocks(root);

        listItem.InlineLayout.ShouldNotBeNull();
        var line = listItem.InlineLayout!.Segments[0].Lines[0];
        line.Rect.X.ShouldBe(12f);
        line.Rect.Width.ShouldBeLessThanOrEqualTo(188f);
    }


    [Fact]
    public void Layout_MixedInlineAndBlock_ProducesAnonymousBlockForInlineRun()
    {
        var root = new BlockBoxBuilder()
            .Inline("root inline")
            .Block(new())
            .Up()
            .BuildRoot();

        NormalizeForBlockLayout(root);


        // Act
        var result = LayoutMutableBlocks(root);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result[0].IsAnonymous.ShouldBeTrue(),
            () => result[0].Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>().TextContent
                .ShouldBe("root inline"),
            () => result[1].IsAnonymous.ShouldBeFalse());
    }

    [Fact]
    public void Layout_AnonymousBlock_DoesNotInheritWidthOrHeightConstraints()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
            {
                WidthPt = 60f,
                MinWidthPt = 50f,
                MaxWidthPt = 70f,
                HeightPt = 40f,
                MinHeightPt = 30f,
                MaxHeightPt = 50f
            }
        };

        root.Children.Add(new InlineBox(BoxRole.Inline) { TextContent = "inline" });
        root.Children.Add(new BlockBox(BoxRole.Block) { Style = new() });

        NormalizeForBlockLayout(root);

        var result = LayoutMutableBlocks(root);

        var anonymous = result.FirstOrDefault(b => b.IsAnonymous);
        anonymous.ShouldNotBeNull();
        anonymous.Style.WidthPt.ShouldBeNull();
        anonymous.Style.MinWidthPt.ShouldBeNull();
        anonymous.Style.MaxWidthPt.ShouldBeNull();
        anonymous.Style.HeightPt.ShouldBeNull();
        anonymous.Style.MinHeightPt.ShouldBeNull();
        anonymous.Style.MaxHeightPt.ShouldBeNull();
    }

    [Fact]
    public void Layout_BlockOnlyChildren_DoesNotCreateAnonymousBlocks()
    {
        var root = new BlockBoxBuilder()
            .Block(new())
            .Up()
            .Block(new())
            .BuildRoot();

        NormalizeForBlockLayout(root);

        // Act
        var result = LayoutMutableBlocks(root);

        // Assert
        result.Count.ShouldBe(2);
        result[0].IsAnonymous.ShouldBeFalse();
        result[1].IsAnonymous.ShouldBeFalse();
    }

    [Theory]
    [InlineData(150f, 150f)] // Smaller than page (200) -> Clamped
    [InlineData(300f, 200f)] // Larger than page (200) -> Page limited
    [InlineData(null, 200f)] // No max width -> Page limited
    public void Layout_RespectsMaxWidth_ClampingToAvailableWidth(float? maxWidthPt, float expectedWidth)
    {
        // Arrange
        var root = new BlockBoxBuilder()
            .Block(new() { MaxWidthPt = maxWidthPt })
            .BuildRoot();

        // Act
        var result = LayoutMutableBlocks(root);

        // Assert
        result.ShouldHaveSingleItem()
            .UsedGeometry
            .ShouldNotBeNull()
            .Width
            .ShouldBe(expectedWidth);
    }

    private BlockBoxLayout CreateBlockBoxLayout(IDiagnosticsSink? diagnosticsSink = null)
    {
        var formattingContext = new BlockContentExtentMeasurement();
        var imageResolver = new ImageSizingRules();
        var inlineEngine = new InlineFlowLayout(
            new FontMetricsProvider(),
            _textMeasurer,
            new DefaultLineHeightStrategy(),
            formattingContext,
            imageResolver,
            diagnosticsSink);
        return new(
            inlineEngine,
            new(inlineEngine, imageResolver),
            formattingContext,
            imageResolver,
            diagnosticsSink);
    }

    private IReadOnlyList<BlockBox> LayoutMutableBlocks(BoxNode root, IDiagnosticsSink? diagnosticsSink = null)
    {
        var page = DefaultPage();
        _ = ResolvePublished(root, page, diagnosticsSink);

        if (root is TableBox tableRoot)
        {
            return [tableRoot];
        }

        if (root is BlockBox rootBlock)
        {
            return rootBlock.Children.OfType<BlockBox>().ToList();
        }

        return [];
    }

    private PublishedLayoutTree ResolvePublished(
        BoxNode root,
        PageBox page,
        IDiagnosticsSink? diagnosticsSink = null) =>
        PublishedLayoutTestResolver.Resolve(CreateBlockBoxLayout(diagnosticsSink), root, page);

    private static void NormalizeForBlockLayout(BoxNode root)
    {
        if (root is BlockBox block)
        {
            BlockFlowNormalization.NormalizeChildrenForBlock(block);
        }
    }

    private static BlockBox CreatePublishedSeamRoot()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        root.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new()
            {
                HeightPt = 20f
            }
        });

        return root;
    }
}