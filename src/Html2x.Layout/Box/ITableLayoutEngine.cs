namespace Html2x.Layout.Box;

public interface ITableLayoutEngine
{
    float MeasureHeight(TableBox table, float availableWidth);
}