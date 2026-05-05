using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class DiagnosticSnapshotValueMapper
{
    internal static DiagnosticObject MapSize(SizePt size) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Width, size.Width),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Height, size.Height));

    internal static DiagnosticObject? MapSize(SizePt? size) =>
        size.HasValue ? MapSize(size.Value) : null;

    internal static DiagnosticObject MapSpacing(Spacing spacing) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Top, spacing.Top),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Right, spacing.Right),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Bottom, spacing.Bottom),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Left, spacing.Left));

    internal static DiagnosticObject? MapSpacing(Spacing? spacing) =>
        spacing.HasValue ? MapSpacing(spacing.Value) : null;

    internal static DiagnosticObject? MapBorders(BorderEdges? borders)
    {
        return borders is null
            ? null
            : DiagnosticObject.Create(
                DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Top, MapBorderSide(borders.Top)),
                DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Right, MapBorderSide(borders.Right)),
                DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Bottom, MapBorderSide(borders.Bottom)),
                DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Left, MapBorderSide(borders.Left)));
    }

    internal static DiagnosticValue? MapColor(ColorRgba? color) =>
        color.HasValue ? DiagnosticValue.From(color.Value.ToHex()) : null;

    internal static DiagnosticValue? FromNullable(string? value) =>
        value is null ? null : DiagnosticValue.From(value);

    internal static DiagnosticValue? FromNullable(float? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    internal static DiagnosticValue? FromNullable(int? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    internal static DiagnosticValue? FromNullable(bool? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    internal static DiagnosticValue? FromNullable<TEnum>(TEnum? value)
        where TEnum : struct, Enum =>
        value.HasValue ? DiagnosticValue.FromEnum(value.Value) : null;

    internal static DiagnosticArray MapArray<T>(
        IEnumerable<T> values,
        Func<T, DiagnosticValue?> mapper)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(mapper);

        return new DiagnosticArray(values.Select(mapper));
    }

    private static DiagnosticObject? MapBorderSide(BorderSide? side)
    {
        return side is null
            ? null
            : DiagnosticObject.Create(
                DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Width, side.Width),
                DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Color, side.Color.ToHex()),
                DiagnosticObject.Field(LayoutSnapshotSchema.Fields.LineStyle, DiagnosticValue.FromEnum(side.LineStyle)));
    }
}
