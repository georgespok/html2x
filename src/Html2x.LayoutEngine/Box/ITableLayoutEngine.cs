using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public interface ITableLayoutEngine
{
    TableLayoutResult Layout(TableBox table, float availableWidth);
}
