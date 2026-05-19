using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Stage.Contracts.Geometry;

/// <summary>
///     Carries per-build inputs required to invoke the Layout Geometry stage.
/// </summary>
internal sealed record LayoutGeometryBuildRequest(
    StyleTree Styles,
    LayoutGeometryRequest Geometry,
    IDiagnosticsSink? DiagnosticsSink);