using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class GeometrySnapshotMapper
{
    public static GeometrySnapshot From(
        PublishedLayoutTree layoutTree,
        HtmlLayout layout,
        PaginationResult pagination)
    {
        ArgumentNullException.ThrowIfNull(layoutTree);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(pagination);

        var boxMapper = new PublishedGeometrySnapshotMapper();

        return new GeometrySnapshot
        {
            Fragments = LayoutSnapshotMapper.From(layout),
            Boxes = boxMapper.MapBoxes(layoutTree.Blocks),
            Pagination = pagination.Pages.Select(MapPage).ToList()
        };
    }

    public static DiagnosticObject ToDiagnosticObject(
        PublishedLayoutTree layoutTree,
        HtmlLayout layout,
        PaginationResult pagination)
    {
        return MapGeometrySnapshot(From(layoutTree, layout, pagination));
    }

    private static DiagnosticObject MapGeometrySnapshot(GeometrySnapshot snapshot)
    {
        return DiagnosticObject.Create(
            DiagnosticObject.Field("fragments", LayoutSnapshotMapper.MapLayoutSnapshot(snapshot.Fragments)),
            DiagnosticObject.Field("boxes", new DiagnosticArray(snapshot.Boxes.Select(MapBoxSnapshot))),
            DiagnosticObject.Field("pagination", new DiagnosticArray(snapshot.Pagination.Select(MapPaginationPage))));
    }

    private static DiagnosticObject MapPaginationPage(PaginationPageSnapshot page)
    {
        return DiagnosticObject.Create(
            DiagnosticObject.Field("pageNumber", page.PageNumber),
            DiagnosticObject.Field("pageSize", LayoutSnapshotMapper.MapSize(page.PageSize)),
            DiagnosticObject.Field("margin", LayoutSnapshotMapper.MapSpacing(page.Margin)),
            DiagnosticObject.Field("contentTop", page.ContentTop),
            DiagnosticObject.Field("contentBottom", page.ContentBottom),
            DiagnosticObject.Field("placements", new DiagnosticArray(page.Placements.Select(MapPlacementSnapshot))));
    }

    private static DiagnosticObject MapPlacementSnapshot(PaginationPlacementSnapshot placement)
    {
        return DiagnosticObject.Create(
            DiagnosticObject.Field("fragmentId", placement.FragmentId),
            DiagnosticObject.Field("kind", placement.Kind),
            DiagnosticObject.Field("pageNumber", placement.PageNumber),
            DiagnosticObject.Field("orderIndex", placement.OrderIndex),
            DiagnosticObject.Field("isOversized", placement.IsOversized),
            DiagnosticObject.Field("x", placement.X),
            DiagnosticObject.Field("y", placement.Y),
            DiagnosticObject.Field("size", LayoutSnapshotMapper.MapSize(placement.Size)),
            DiagnosticObject.Field("displayRole", FromNullable(placement.DisplayRole)),
            DiagnosticObject.Field("formattingContext", FromNullable(placement.FormattingContext)),
            DiagnosticObject.Field("markerOffset", FromNullable(placement.MarkerOffset)),
            DiagnosticObject.Field("derivedColumnCount", FromNullable(placement.DerivedColumnCount)),
            DiagnosticObject.Field("rowIndex", FromNullable(placement.RowIndex)),
            DiagnosticObject.Field("columnIndex", FromNullable(placement.ColumnIndex)),
            DiagnosticObject.Field("isHeader", FromNullable(placement.IsHeader)),
            DiagnosticObject.Field("metadataOwner", FromNullable(placement.MetadataOwner)),
            DiagnosticObject.Field("metadataConsumer", FromNullable(placement.MetadataConsumer)));
    }

    private static DiagnosticObject MapBoxSnapshot(BoxGeometrySnapshot box)
    {
        return DiagnosticObject.Create(
            DiagnosticObject.Field("sequenceId", box.SequenceId),
            DiagnosticObject.Field("path", box.Path),
            DiagnosticObject.Field("kind", box.Kind),
            DiagnosticObject.Field("tagName", FromNullable(box.TagName)),
            DiagnosticObject.Field("sourceNodeId", FromNullable(box.SourceNodeId)),
            DiagnosticObject.Field("sourceContentId", FromNullable(box.SourceContentId)),
            DiagnosticObject.Field("sourcePath", FromNullable(box.SourcePath)),
            DiagnosticObject.Field("sourceOrder", FromNullable(box.SourceOrder)),
            DiagnosticObject.Field("sourceElementIdentity", FromNullable(box.SourceElementIdentity)),
            DiagnosticObject.Field("generatedSourceKind", FromNullable(box.GeneratedSourceKind)),
            DiagnosticObject.Field("x", box.X),
            DiagnosticObject.Field("y", box.Y),
            DiagnosticObject.Field("size", LayoutSnapshotMapper.MapSize(box.Size)),
            DiagnosticObject.Field("contentX", FromNullable(box.ContentX)),
            DiagnosticObject.Field("contentY", FromNullable(box.ContentY)),
            DiagnosticObject.Field("contentSize", LayoutSnapshotMapper.MapSize(box.ContentSize)),
            DiagnosticObject.Field("baseline", FromNullable(box.Baseline)),
            DiagnosticObject.Field("markerOffset", box.MarkerOffset),
            DiagnosticObject.Field("allowsOverflow", box.AllowsOverflow),
            DiagnosticObject.Field("isAnonymous", box.IsAnonymous),
            DiagnosticObject.Field("isInlineBlockContext", box.IsInlineBlockContext),
            DiagnosticObject.Field("derivedColumnCount", FromNullable(box.DerivedColumnCount)),
            DiagnosticObject.Field("rowIndex", FromNullable(box.RowIndex)),
            DiagnosticObject.Field("columnIndex", FromNullable(box.ColumnIndex)),
            DiagnosticObject.Field("isHeader", FromNullable(box.IsHeader)),
            DiagnosticObject.Field("metadataOwner", FromNullable(box.MetadataOwner)),
            DiagnosticObject.Field("metadataConsumer", FromNullable(box.MetadataConsumer)),
            DiagnosticObject.Field("children", new DiagnosticArray(box.Children.Select(MapBoxSnapshot))));
    }

    private static DiagnosticValue? FromNullable(string? value) =>
        value is null ? null : DiagnosticValue.From(value);

    private static DiagnosticValue? FromNullable(float? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable(int? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable(bool? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable<TEnum>(TEnum? value)
        where TEnum : struct, Enum =>
        value.HasValue ? DiagnosticValue.FromEnum(value.Value) : null;

    private static PaginationPageSnapshot MapPage(PageModel page)
    {
        return new PaginationPageSnapshot
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            Margin = page.Margin,
            ContentTop = page.ContentTop,
            ContentBottom = page.ContentBottom,
            Placements = page.Placements.Select(MapPlacement).ToList()
        };
    }

    private static PaginationPlacementSnapshot MapPlacement(BlockFragmentPlacement placement)
    {
        var fragment = placement.Fragment;

        return new PaginationPlacementSnapshot
        {
            FragmentId = placement.FragmentId,
            Kind = fragment.DisplayRole?.ToString() ?? fragment.GetType().Name,
            PageNumber = placement.PageNumber,
            OrderIndex = placement.OrderIndex,
            IsOversized = placement.IsOversized,
            X = placement.PageX,
            Y = placement.PageY,
            Size = new SizePt(placement.Width, placement.Height),
            DisplayRole = fragment.DisplayRole,
            FormattingContext = fragment.FormattingContext,
            MarkerOffset = fragment.MarkerOffset,
            DerivedColumnCount = fragment is TableFragment table ? table.DerivedColumnCount : null,
            RowIndex = fragment is TableRowFragment row ? row.RowIndex : null,
            ColumnIndex = fragment is TableCellFragment cell ? cell.ColumnIndex : null,
            IsHeader = fragment is TableCellFragment headerCell ? headerCell.IsHeader : null,
            MetadataOwner = PublishedLayoutToFragmentProjector.MetadataOwnerName,
            MetadataConsumer = nameof(FragmentPlacementCloner)
        };
    }

    private sealed class PublishedGeometrySnapshotMapper
    {
        private const string BoxGeometryOwnerName = "BlockLayoutEngine";
        private int _sequenceId;

        public IReadOnlyList<BoxGeometrySnapshot> MapBoxes(IEnumerable<PublishedBlock> blocks)
        {
            var snapshots = new List<BoxGeometrySnapshot>();
            foreach (var block in blocks)
            {
                snapshots.Add(MapBox(block));
            }

            return snapshots;
        }

        private BoxGeometrySnapshot MapBox(PublishedBlock block)
        {
            var borderRect = block.Geometry.BorderBoxRect;
            var contentRect = block.Geometry.ContentBoxRect;

            return new BoxGeometrySnapshot
            {
                SequenceId = NextSequenceId(),
                Path = block.Identity.NodePath,
                Kind = block.Display.Role.ToString().ToLowerInvariant(),
                TagName = ResolveTagName(block.Identity.ElementIdentity),
                SourceNodeId = ResolveSourceNodeId(block.Identity.SourceIdentity),
                SourceContentId = ResolveSourceContentId(block.Identity.SourceIdentity),
                SourcePath = block.Identity.SourceIdentity.SourcePath,
                SourceOrder = block.Identity.SourceIdentity.SourceOrder,
                SourceElementIdentity = block.Identity.SourceIdentity.ElementIdentity,
                GeneratedSourceKind = ResolveGeneratedSourceKind(block.Identity.SourceIdentity.GeneratedKind),
                X = borderRect.X,
                Y = borderRect.Y,
                Size = new SizePt(borderRect.Width, borderRect.Height),
                ContentX = contentRect.X,
                ContentY = contentRect.Y,
                ContentSize = new SizePt(contentRect.Width, contentRect.Height),
                Baseline = block.Geometry.Baseline,
                MarkerOffset = block.Geometry.MarkerOffset,
                AllowsOverflow = block.Geometry.AllowsOverflow,
                IsAnonymous = false,
                IsInlineBlockContext = block.Display.FormattingContext == FormattingContextKind.InlineBlock,
                DerivedColumnCount = block.Table?.DerivedColumnCount,
                RowIndex = block.Table?.RowIndex,
                ColumnIndex = block.Table?.ColumnIndex,
                IsHeader = block.Table?.IsHeader,
                MetadataOwner = BoxGeometryOwnerName,
                MetadataConsumer = nameof(GeometrySnapshotMapper),
                Children = MapBoxes(block.Children)
            };
        }

        private static string? ResolveTagName(string? elementIdentity)
        {
            if (string.IsNullOrWhiteSpace(elementIdentity))
            {
                return null;
            }

            var markerIndex = elementIdentity.IndexOfAny(['#', '.']);
            return markerIndex > 0
                ? elementIdentity[..markerIndex]
                : elementIdentity;
        }

        private static int? ResolveSourceNodeId(GeometrySourceIdentity identity)
        {
            var nodeId = identity.NodeId;
            return nodeId.HasValue && nodeId.Value.IsSpecified
                ? nodeId.Value.Value
                : null;
        }

        private static int? ResolveSourceContentId(GeometrySourceIdentity identity)
        {
            var contentId = identity.ContentId;
            return contentId.HasValue && contentId.Value.IsSpecified
                ? contentId.Value.Value
                : null;
        }

        private static string? ResolveGeneratedSourceKind(GeometryGeneratedSourceKind generatedKind)
        {
            return generatedKind switch
            {
                GeometryGeneratedSourceKind.None => null,
                GeometryGeneratedSourceKind.AnonymousText => "anonymous-text",
                GeometryGeneratedSourceKind.ListMarker => "list-marker",
                GeometryGeneratedSourceKind.InlineBlockContent => "inline-block-content",
                GeometryGeneratedSourceKind.AnonymousBlock => "anonymous-block",
                GeometryGeneratedSourceKind.InlineBlockBoundary => "inline-block-boundary",
                GeometryGeneratedSourceKind.InlineSegment => "inline-segment",
                _ => "generated"
            };
        }

        private int NextSequenceId()
        {
            _sequenceId++;
            return _sequenceId;
        }
    }
}
