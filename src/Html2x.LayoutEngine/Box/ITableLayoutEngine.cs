using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public interface ITableLayoutEngine
{
    float MeasureHeight(TableBox table, float availableWidth);
}