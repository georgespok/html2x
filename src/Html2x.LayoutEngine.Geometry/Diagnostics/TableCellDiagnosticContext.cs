using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Diagnostics;


internal sealed record TableCellDiagnosticContext(
    int RowIndex,
    int ColumnIndex,
    bool IsHeader,
    float Width,
    float Height);
