using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Builds the display tree separately from box geometry so pipeline stages can own distinct layout responsibilities.
/// </summary>
public interface IBoxTreeBuilder
{
    DisplayNode BuildDisplayTree(StyleTree styles, DiagnosticsSession? diagnosticsSession = null);

    BoxTree BuildLayoutGeometry(
        DisplayNode displayRoot,
        StyleTree styles,
        DiagnosticsSession? diagnosticsSession = null,
        BoxTreeBuildContext? context = null);

    BoxTree Build(StyleTree styles, DiagnosticsSession? diagnosticsSession = null, BoxTreeBuildContext? context = null);
}
