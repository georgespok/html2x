using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

/// <summary>
///     Verifies inline object placement publishes geometry and nested inline layout.
/// </summary>
public sealed class AtomicInlineObjectLayoutWriterTests
{
    [Fact]
    public void Write_InlineObject_AppliesGeometryAndNestedInlineLayoutToContentBox()
    {
        var nestedLayout = new TextLayoutResult([], 4f, 6f);
        var contentBox = new BlockBox(BoxRole.Block)
        {
            Style = new()
            {
                Padding = new(1f, 2f, 3f, 4f),
                Margin = new(5f, 6f, 7f, 8f),
                TextAlign = "right",
                Borders = BorderEdges.Uniform(new(1f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        var inlineObject = new InlineObjectLayout(
            contentBox,
            nestedLayout,
            8f,
            4f,
            20f,
            10f,
            7f);
        BlockBox? capturedBlock = null;
        TextLayoutResult? capturedLayout = null;
        var capturedLeft = 0f;
        var capturedTop = 0f;
        var capturedWidth = 0f;
        string? capturedAlign = null;
        var writer = new AtomicInlineObjectLayoutWriter(
            (block, layout, left, top, width, align) =>
            {
                capturedBlock = block;
                capturedLayout = layout;
                capturedLeft = left;
                capturedTop = top;
                capturedWidth = width;
                capturedAlign = align;
                return new([], top, 0f);
            },
            new());

        var rect = writer.Write(inlineObject, 30f, 50f);

        rect.ShouldBe(new(30f, 43f, 20f, 10f));
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