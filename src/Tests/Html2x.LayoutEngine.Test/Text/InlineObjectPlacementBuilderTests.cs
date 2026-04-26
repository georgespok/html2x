using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

/// <summary>
/// Verifies inline object placement publishes geometry and nested inline layout.
/// </summary>
public sealed class InlineObjectPlacementBuilderTests
{
    [Fact]
    public void Place_InlineObject_AppliesGeometryAndNestedInlineLayoutToContentBox()
    {
        var nestedLayout = new TextLayoutResult([], TotalHeight: 4f, MaxLineWidth: 6f);
        var contentBox = new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle
            {
                Padding = new Spacing(1f, 2f, 3f, 4f),
                Margin = new Spacing(5f, 6f, 7f, 8f),
                TextAlign = "right",
                Borders = BorderEdges.Uniform(new BorderSide(1f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        var inlineObject = new InlineObjectLayout(
            contentBox,
            nestedLayout,
            ContentWidth: 8f,
            ContentHeight: 4f,
            BorderBoxWidth: 20f,
            BorderBoxHeight: 10f,
            Baseline: 7f);
        BlockBox? capturedBlock = null;
        TextLayoutResult? capturedLayout = null;
        float capturedLeft = 0f;
        float capturedTop = 0f;
        float capturedWidth = 0f;
        string? capturedAlign = null;
        var builder = new InlineObjectPlacementBuilder((block, layout, left, top, width, align) =>
        {
            capturedBlock = block;
            capturedLayout = layout;
            capturedLeft = left;
            capturedTop = top;
            capturedWidth = width;
            capturedAlign = align;
            return new InlineFlowSegmentLayout([], top, 0f);
        });

        var rect = builder.Place(inlineObject, left: 30f, baselineY: 50f);

        rect.ShouldBe(new RectangleF(30f, 43f, 20f, 10f));
        contentBox.UsedGeometry.ShouldNotBeNull();
        contentBox.UsedGeometry.Value.BorderBoxRect.ShouldBe(rect);
        contentBox.Padding.ShouldBe(contentBox.Style.Padding);
        contentBox.Margin.ShouldBe(contentBox.Style.Margin);
        contentBox.TextAlign.ShouldBe("right");
        contentBox.InlineLayout.ShouldNotBeNull();
        contentBox.InlineLayout.Segments.ShouldHaveSingleItem().Top.ShouldBe(capturedTop);
        capturedBlock.ShouldBeSameAs(contentBox);
        capturedLayout.ShouldBeSameAs(nestedLayout);
        capturedLeft.ShouldBe(35f);
        capturedTop.ShouldBe(45f);
        capturedWidth.ShouldBe(12f);
        capturedAlign.ShouldBe("right");
    }
}
