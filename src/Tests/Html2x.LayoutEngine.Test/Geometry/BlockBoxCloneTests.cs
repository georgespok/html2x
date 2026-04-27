using System.Collections.Generic;
using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;
using Shouldly;
using Xunit;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class BlockBoxCloneTests
{
    public static IEnumerable<object[]> BlockFactories()
    {
        yield return [new Func<BlockBox>(() => new BlockBox(BoxRole.Block) { IsAnonymous = true })];
        yield return [new Func<BlockBox>(() => new ImageBox(BoxRole.Block) { IsAnonymous = true })];
        yield return [new Func<BlockBox>(() => new InlineBlockBoundaryBox(
            new InlineBox(BoxRole.InlineBlock),
            new BlockBox(BoxRole.Block)) { IsAnonymous = true })];
        yield return [new Func<BlockBox>(() => new RuleBox(BoxRole.Block) { IsAnonymous = true })];
        yield return [new Func<BlockBox>(() => new TableBox(BoxRole.Table) { IsAnonymous = true })];
        yield return [new Func<BlockBox>(() => new TableRowBox(BoxRole.TableRow) { IsAnonymous = true })];
        yield return [new Func<BlockBox>(() => new TableCellBox(BoxRole.TableCell) { IsAnonymous = true })];
    }

    [Theory]
    [MemberData(nameof(BlockFactories))]
    public void CloneForParent_BlockLikeSubclass_PreservesCommonGeometryState(Func<BlockBox> createBlock)
    {
        var parent = new BlockBox(BoxRole.Block);
        var source = createBlock();
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
        clone.X.ShouldBe(source.X);
        clone.Y.ShouldBe(source.Y);
        clone.Width.ShouldBe(source.Width);
        clone.Height.ShouldBe(source.Height);
        clone.MarkerOffset.ShouldBe(source.MarkerOffset);
    }

    [Fact]
    public void CloneForParent_PreLayoutCompatibilityGeometry_PreservesSeedValues()
    {
        var parent = new BlockBox(BoxRole.Block);
        var source = new BlockBox(BoxRole.Block)
        {
            X = 11f,
            Y = 12f,
            Width = 13f,
            Height = 14f,
            MarkerOffset = 2f
        };

        var clone = source.CloneForParent(parent).ShouldBeAssignableTo<BlockBox>();

        clone.UsedGeometry.ShouldBeNull();
        clone.X.ShouldBe(source.X);
        clone.Y.ShouldBe(source.Y);
        clone.Width.ShouldBe(source.Width);
        clone.Height.ShouldBe(source.Height);
        clone.MarkerOffset.ShouldBe(source.MarkerOffset);
    }
}
