using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class BlockBoxCloneTests
{
    public static IEnumerable<object[]> BlockFactories()
    {
        yield return [new Func<GeometrySourceIdentity, BlockBox>(identity => new BlockBox(BoxRole.Block) { IsAnonymous = true, SourceIdentity = identity })];
        yield return [new Func<GeometrySourceIdentity, BlockBox>(identity => new ImageBox(BoxRole.Block) { IsAnonymous = true, SourceIdentity = identity })];
        yield return [new Func<GeometrySourceIdentity, BlockBox>(identity => new InlineBlockBoundaryBox(
            new InlineBox(BoxRole.InlineBlock),
            new BlockBox(BoxRole.Block)) { IsAnonymous = true, SourceIdentity = identity })];
        yield return [new Func<GeometrySourceIdentity, BlockBox>(identity => new RuleBox(BoxRole.Block) { IsAnonymous = true, SourceIdentity = identity })];
        yield return [new Func<GeometrySourceIdentity, BlockBox>(identity => new TableBox(BoxRole.Table) { IsAnonymous = true, SourceIdentity = identity })];
        yield return [new Func<GeometrySourceIdentity, BlockBox>(identity => new TableRowBox(BoxRole.TableRow) { IsAnonymous = true, SourceIdentity = identity })];
        yield return [new Func<GeometrySourceIdentity, BlockBox>(identity => new TableCellBox(BoxRole.TableCell) { IsAnonymous = true, SourceIdentity = identity })];
    }

    [Theory]
    [MemberData(nameof(BlockFactories))]
    public void CloneForParent_BlockLikeSubclass_PreservesCommonGeometryState(object createBlockFactory)
    {
        var createBlock = createBlockFactory.ShouldBeOfType<Func<GeometrySourceIdentity, BlockBox>>();
        var parent = new BlockBox(BoxRole.Block);
        var sourceIdentity = CreateSourceIdentity();
        var source = createBlock(sourceIdentity);
        var style = new ComputedStyle { FontSizePt = 16f };
        var geometry = BoxGeometryFactory.FromBorderBox(
            new RectangleF(10f, 20f, 120f, 40f),
            new Spacing(3f, 4f, 5f, 6f),
            new Spacing(1f, 2f, 3f, 4f),
            baseline: 34f,
            markerOffset: 7f);

        source.Style = style;
        source.Margin = new Spacing(1f, 2f, 3f, 4f);
        source.Padding = new Spacing(3f, 4f, 5f, 6f);
        source.TextAlign = "center";
        source.IsInlineBlockContext = true;
        source.MarkerOffset = 7f;
        source.ApplyLayoutGeometry(geometry);

        var clone = source.CloneForParent(parent).ShouldBeAssignableTo<BlockBox>();

        clone.GetType().ShouldBe(source.GetType());
        clone.Parent.ShouldBeSameAs(parent);
        clone.Style.ShouldBeSameAs(style);
        clone.Margin.ShouldBe(source.Margin);
        clone.Padding.ShouldBe(source.Padding);
        clone.TextAlign.ShouldBe(source.TextAlign);
        clone.IsAnonymous.ShouldBe(source.IsAnonymous);
        clone.IsInlineBlockContext.ShouldBe(source.IsInlineBlockContext);
        clone.UsedGeometry.ShouldBe(source.UsedGeometry);
        clone.MarkerOffset.ShouldBe(source.MarkerOffset);
        clone.SourceIdentity.ShouldBe(sourceIdentity);
    }

    [Fact]
    public void CloneForParent_PreLayoutMarkerOffset_PreservesLayoutInput()
    {
        var parent = new BlockBox(BoxRole.Block);
        var source = new BlockBox(BoxRole.Block)
        {
            MarkerOffset = 2f
        };

        var clone = source.CloneForParent(parent).ShouldBeAssignableTo<BlockBox>();

        clone.UsedGeometry.ShouldBeNull();
        clone.MarkerOffset.ShouldBe(source.MarkerOffset);
    }

    [Fact]
    public void CloneForParent_InlineBox_PreservesSourceIdentity()
    {
        var sourceIdentity = CreateSourceIdentity();
        var parent = new BlockBox(BoxRole.Block);
        var source = new InlineBox(BoxRole.Inline)
        {
            SourceIdentity = sourceIdentity,
            TextContent = "source"
        };

        var clone = source.CloneForParent(parent).ShouldBeOfType<InlineBox>();

        clone.SourceIdentity.ShouldBe(sourceIdentity);
        clone.Parent.ShouldBeSameAs(parent);
        clone.TextContent.ShouldBe("source");
    }

    [Fact]
    public void CloneForParent_TableSectionBox_PreservesSourceIdentity()
    {
        var sourceIdentity = CreateSourceIdentity();
        var parent = new TableBox(BoxRole.Table);
        var source = new TableSectionBox(BoxRole.TableSection)
        {
            SourceIdentity = sourceIdentity
        };

        var clone = source.CloneForParent(parent).ShouldBeOfType<TableSectionBox>();

        clone.SourceIdentity.ShouldBe(sourceIdentity);
        clone.Parent.ShouldBeSameAs(parent);
    }

    [Fact]
    public void CloneForParent_FloatBox_PreservesSourceIdentity()
    {
        var sourceIdentity = CreateSourceIdentity();
        var parent = new BlockBox(BoxRole.Block);
        var source = new FloatBox(BoxRole.Float)
        {
            SourceIdentity = sourceIdentity,
            FloatDirection = HtmlCssConstants.CssValues.Right
        };

        var clone = source.CloneForParent(parent).ShouldBeOfType<FloatBox>();

        clone.SourceIdentity.ShouldBe(sourceIdentity);
        clone.Parent.ShouldBeSameAs(parent);
        clone.FloatDirection.ShouldBe(HtmlCssConstants.CssValues.Right);
    }

    private static GeometrySourceIdentity CreateSourceIdentity()
    {
        return new GeometrySourceIdentity(
            new StyleNodeId(12),
            null,
            "body[0]/div[0]",
            "div",
            4,
            GeometryGeneratedSourceKind.None);
    }
}
