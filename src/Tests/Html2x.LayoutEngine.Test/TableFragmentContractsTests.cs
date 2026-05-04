using Html2x.RenderModel;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public class TableFragmentContractsTests
{
    [Fact]
    public void TableFragmentFamily_PreservesHierarchyAndTableSpecificFacts()
    {
        var line = new LineBoxFragment
        {
            Rect = new RectPt(12f, 18f, 40f, 10f),
            Runs = []
        };

        var cell = new TableCellFragment([line])
        {
            Rect = new RectPt(10f, 16f, 80f, 24f),
            ColumnIndex = 1,
            IsHeader = true
        };

        var row = new TableRowFragment([cell])
        {
            Rect = new RectPt(8f, 14f, 160f, 28f),
            RowIndex = 0
        };

        var table = new TableFragment([row])
        {
            Rect = new RectPt(6f, 12f, 160f, 56f),
            DerivedColumnCount = 2
        };

        table.DerivedColumnCount.ShouldBe(2);
        table.Rows.ShouldHaveSingleItem().RowIndex.ShouldBe(0);
        table.Rows[0].Cells.ShouldHaveSingleItem().ColumnIndex.ShouldBe(1);
        table.Rows[0].Cells[0].IsHeader.ShouldBeTrue();
        table.Rows[0].Cells[0].Children.ShouldHaveSingleItem().ShouldBeSameAs(line);
    }

    [Fact]
    public void TableFragmentFamily_ReusesTypedChildViews()
    {
        var cell = new TableCellFragment();
        var row = new TableRowFragment([cell]);
        var table = new TableFragment([row]);

        table.Rows.ShouldBeSameAs(table.Rows);
        row.Cells.ShouldBeSameAs(row.Cells);
    }
}
