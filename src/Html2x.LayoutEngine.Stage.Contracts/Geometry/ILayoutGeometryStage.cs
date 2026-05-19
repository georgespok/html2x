using Html2x.LayoutEngine.Contracts.Published;

namespace Html2x.LayoutEngine.Stage.Contracts.Geometry;

/// <summary>
///     Defines the composition-facing invocation contract for the Layout Geometry stage.
/// </summary>
/// <remarks>
///     This interface is an execution contract, not a layout fact contract.
///     Implementations consume style and geometry input facts, may emit
///     diagnostics for one build, and return the published layout handoff.
/// </remarks>
internal interface ILayoutGeometryStage
{
    PublishedLayoutTree Build(LayoutGeometryBuildRequest request);
}