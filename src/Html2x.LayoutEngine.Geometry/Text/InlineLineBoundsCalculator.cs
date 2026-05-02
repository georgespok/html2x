using System.Drawing;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

/// <summary>
/// Calculates full line slots and tight occupied rectangles from already placed inline items.
/// </summary>
internal sealed class InlineLineBoundsCalculator
{
    public RectangleF CreateLineSlotRect(
        IReadOnlyList<InlineLineItemLayout> items,
        float contentLeft,
        float contentWidth,
        float topY,
        float lineHeight)
    {
        var occupiedRect = CreateLineOccupiedRect(items, contentLeft, topY, lineHeight);
        var slotWidth = float.IsFinite(contentWidth)
            ? Math.Max(0f, contentWidth)
            : occupiedRect.Width;

        return new RectangleF(contentLeft, topY, slotWidth, lineHeight);
    }

    public RectangleF CreateLineOccupiedRect(
        IReadOnlyList<InlineLineItemLayout> items,
        float contentLeft,
        float topY,
        float lineHeight)
    {
        if (items.Count == 0)
        {
            return new RectangleF(contentLeft, topY, 0f, lineHeight);
        }

        var minX = items.Min(static item => item.Rect.X);
        var maxX = items.Max(static item => item.Rect.Right);
        return new RectangleF(minX, topY, Math.Max(0f, maxX - minX), lineHeight);
    }

    public RectangleF CreateTextItemRect(
        IReadOnlyList<TextRun> runs,
        float contentLeft,
        float topY,
        float lineHeight)
    {
        var minX = runs.Min(static run => run.Origin.X);
        var maxX = runs.Max(static run => run.Origin.X + run.AdvanceWidth);
        return new RectangleF(minX, topY, Math.Max(0f, maxX - minX), lineHeight);
    }
}
