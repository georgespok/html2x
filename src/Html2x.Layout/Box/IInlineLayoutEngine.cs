namespace Html2x.Layout.Box;

public interface IInlineLayoutEngine
{
    float MeasureHeight(DisplayNode block, float availableWidth);
}