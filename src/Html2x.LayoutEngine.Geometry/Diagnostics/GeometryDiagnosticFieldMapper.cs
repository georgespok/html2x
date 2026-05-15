using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class GeometryDiagnosticFieldMapper
{
    public static DiagnosticFields UnsupportedStructureFields(
        string nodePath,
        string structureKind,
        string reason,
        FormattingContextKind formattingContext) =>
        DiagnosticFields.Create(
            DiagnosticFields.Field(GeometryDiagnosticNames.Fields.NodePath, nodePath),
            DiagnosticFields.Field(GeometryDiagnosticNames.Fields.StructureKind, structureKind),
            DiagnosticFields.Field(GeometryDiagnosticNames.Fields.Reason, reason),
            DiagnosticFields.Field(
                GeometryDiagnosticNames.Fields.FormattingContext,
                DiagnosticValue.FromEnum(formattingContext)));

    public static DiagnosticValue? FromNullable(int? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    public static DiagnosticValue? FromNullable(float? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;
}
