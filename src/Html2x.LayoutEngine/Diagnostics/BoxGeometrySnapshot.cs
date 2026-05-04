using Html2x.RenderModel;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;


internal sealed class BoxGeometrySnapshot
{
    public int SequenceId { get; init; }

    public string Path { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? TagName { get; init; }

    public int? SourceNodeId { get; init; }

    public int? SourceContentId { get; init; }

    public string? SourcePath { get; init; }

    public int? SourceOrder { get; init; }

    public string? SourceElementIdentity { get; init; }

    public string? GeneratedSourceKind { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public SizePt Size { get; init; }

    public float? ContentX { get; init; }

    public float? ContentY { get; init; }

    public SizePt? ContentSize { get; init; }

    public float? Baseline { get; init; }

    public float MarkerOffset { get; init; }

    public bool AllowsOverflow { get; init; }

    public bool IsAnonymous { get; init; }

    public bool IsInlineBlockContext { get; init; }

    public int? DerivedColumnCount { get; init; }

    public int? RowIndex { get; init; }

    public int? ColumnIndex { get; init; }

    public bool? IsHeader { get; init; }

    public string? MetadataOwner { get; init; }

    public string? MetadataConsumer { get; init; }

    public IReadOnlyList<BoxGeometrySnapshot> Children { get; init; } = [];
}
