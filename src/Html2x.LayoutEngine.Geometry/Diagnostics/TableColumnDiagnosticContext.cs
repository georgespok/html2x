using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Diagnostics;


internal sealed record TableColumnDiagnosticContext(
    int ColumnIndex,
    float Width);
