using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

/// <summary>
/// Verifies inline line and text item bounds calculations.
/// </summary>
public sealed class InlineLineBoundsCalculatorTests
{
    private readonly InlineLineBoundsCalculator _calculator = new();

    [Fact]
    public void CreateLineSlotRect_NoItems_ReturnsFullContentSlot()
    {
        var rect = _calculator.CreateLineSlotRect([], contentLeft: 10f, contentWidth: 80f, topY: 20f, lineHeight: 12f);

        rect.ShouldBe(new RectPt(10f, 20f, 80f, 12f));
    }

    [Fact]
    public void CreateLineOccupiedRect_Items_UsesMinAndMaxItemBounds()
    {
        var items = new InlineLineItemLayout[]
        {
            new InlineTextItemLayout(0, new RectPt(15f, 20f, 20f, 12f), [], []),
            new InlineObjectItemLayout(1, new RectPt(50f, 20f, 10f, 12f), new BlockBox(BoxRole.Block))
        };

        var rect = _calculator.CreateLineOccupiedRect(items, contentLeft: 10f, topY: 20f, lineHeight: 12f);

        rect.ShouldBe(new RectPt(15f, 20f, 45f, 12f));
    }

    [Fact]
    public void CreateTextItemRect_Runs_UsesRunOriginsAndAdvanceWidths()
    {
        var runs = new[]
        {
            CreateRun(20f, 30f),
            CreateRun(55f, 10f)
        };

        var rect = _calculator.CreateTextItemRect(runs, contentLeft: 10f, topY: 5f, lineHeight: 14f);

        rect.ShouldBe(new RectPt(20f, 5f, 45f, 14f));
    }

    private static TextRun CreateRun(float x, float width)
    {
        return new TextRun(
            "x",
            new FontKey("Arial", FontWeight.W400, FontStyle.Normal),
            12f,
            new PointPt(x, 10f),
            width,
            9f,
            3f);
    }
}
