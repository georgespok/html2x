namespace Html2x.Layout.Box;

public sealed class InlineLayoutEngine : IInlineLayoutEngine
{
    public float MeasureHeight(DisplayNode block, float availableWidth)
    {
        // MVP: approximate line height as 1.2 * font-size
        var fs = block.Style.FontSizePt > 0 ? block.Style.FontSizePt : 12f;
        return fs * 1.2f;
    }
}