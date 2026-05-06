using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Pagination;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using static Html2x.LayoutEngine.Diagnostics.DiagnosticSnapshotValueMapper;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class GeometrySnapshotMapper
{
    public static GeometrySnapshot From(
        PublishedLayoutTree layoutTree,
        PaginationResult pagination)
    {
        ArgumentNullException.ThrowIfNull(layoutTree);
        ArgumentNullException.ThrowIfNull(pagination);

        var boxMapper = new PublishedGeometrySnapshotMapper();

        return new()
        {
            Fragments = LayoutSnapshotMapper.From(pagination.Layout),
            Boxes = boxMapper.MapBoxes(layoutTree.Blocks),
            Pagination = pagination.AuditPages.Select(MapPage).ToList()
        };
    }

    public static DiagnosticObject ToDiagnosticObject(
        PublishedLayoutTree layoutTree,
        PaginationResult pagination) =>
        MapGeometrySnapshot(From(layoutTree, pagination));

    private static DiagnosticObject MapGeometrySnapshot(GeometrySnapshot snapshot) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.Fragments,
                LayoutSnapshotMapper.MapLayoutSnapshot(snapshot.Fragments)),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.Boxes, MapArray(snapshot.Boxes, MapBoxSnapshot)),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.Pagination,
                MapArray(snapshot.Pagination, MapPaginationPage)));

    private static DiagnosticObject MapPaginationPage(PaginationPageSnapshot page) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.PageNumber, page.PageNumber),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.PageSize, MapSize(page.PageSize)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Margin, MapSpacing(page.Margin)),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.ContentTop, page.ContentTop),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.ContentBottom, page.ContentBottom),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.Placements,
                MapArray(page.Placements, MapPlacementSnapshot)));

    private static DiagnosticObject MapPlacementSnapshot(PaginationPlacementSnapshot placement) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.FragmentId, placement.FragmentId),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Kind, placement.Kind),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.PageNumber, placement.PageNumber),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.OrderIndex, placement.OrderIndex),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.IsOversized, placement.IsOversized),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.DecisionKind,
                DiagnosticValue.FromEnum(placement.DecisionKind)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.X, placement.X),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Y, placement.Y),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Size, MapSize(placement.Size)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.DisplayRole,
                FromNullable(placement.DisplayRole)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.FormattingContext,
                FromNullable(placement.FormattingContext)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MarkerOffset,
                FromNullable(placement.MarkerOffset)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.DerivedColumnCount,
                FromNullable(placement.DerivedColumnCount)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.RowIndex,
                FromNullable(placement.RowIndex)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.ColumnIndex,
                FromNullable(placement.ColumnIndex)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.IsHeader,
                FromNullable(placement.IsHeader)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MetadataOwner,
                FromNullable(placement.MetadataOwner)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MetadataConsumer,
                FromNullable(placement.MetadataConsumer)));

    private static DiagnosticObject MapBoxSnapshot(BoxGeometrySnapshot box) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.SequenceId, box.SequenceId),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.Path, box.Path),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Kind, box.Kind),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.TagName, FromNullable(box.TagName)),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.SourceNodeId,
                FromNullable(box.SourceNodeId)),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.SourceContentId,
                FromNullable(box.SourceContentId)),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.SourcePath,
                FromNullable(box.SourcePath)),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.SourceOrder,
                FromNullable(box.SourceOrder)),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.SourceElementIdentity,
                FromNullable(box.SourceElementIdentity)),
            DiagnosticObject.Field(
                GeometrySnapshotSchema.Fields.GeneratedSourceKind,
                FromNullable(box.GeneratedSourceKind)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.X, box.X),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Y, box.Y),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Size, MapSize(box.Size)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.ContentX, FromNullable(box.ContentX)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.ContentY, FromNullable(box.ContentY)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.ContentSize, MapSize(box.ContentSize)),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.Baseline, FromNullable(box.Baseline)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.MarkerOffset, box.MarkerOffset),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.AllowsOverflow, box.AllowsOverflow),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.IsAnonymous, box.IsAnonymous),
            DiagnosticObject.Field(GeometrySnapshotSchema.Fields.IsInlineBlockContext, box.IsInlineBlockContext),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.DerivedColumnCount,
                FromNullable(box.DerivedColumnCount)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.RowIndex, FromNullable(box.RowIndex)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.ColumnIndex,
                FromNullable(box.ColumnIndex)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.IsHeader, FromNullable(box.IsHeader)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MetadataOwner,
                FromNullable(box.MetadataOwner)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MetadataConsumer,
                FromNullable(box.MetadataConsumer)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.Children,
                MapArray(box.Children, MapBoxSnapshot)));

    private static PaginationPageSnapshot MapPage(PaginationPageAudit page) =>
        new()
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            Margin = page.Margin,
            ContentTop = page.ContentTop,
            ContentBottom = page.ContentBottom,
            Placements = page.Placements.Select(MapPlacement).ToList()
        };

    private static PaginationPlacementSnapshot MapPlacement(PaginationPlacementAudit placement) =>
        new()
        {
            FragmentId = placement.FragmentId,
            Kind = placement.FragmentKind,
            PageNumber = placement.PageNumber,
            OrderIndex = placement.OrderIndex,
            IsOversized = placement.IsOversized,
            DecisionKind = placement.DecisionKind,
            X = placement.PageX,
            Y = placement.PageY,
            Size = new(placement.Width, placement.Height),
            DisplayRole = placement.DisplayRole,
            FormattingContext = placement.FormattingContext,
            MarkerOffset = placement.MarkerOffset,
            DerivedColumnCount = placement.DerivedColumnCount,
            RowIndex = placement.RowIndex,
            ColumnIndex = placement.ColumnIndex,
            IsHeader = placement.IsHeader,
            MetadataOwner = PublishedLayoutToFragmentProjector.MetadataOwnerName,
            MetadataConsumer = GeometrySnapshotSchema.Metadata.PaginationConsumer
        };

    private sealed class PublishedGeometrySnapshotMapper
    {
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

            return new()
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
                Size = new(borderRect.Width, borderRect.Height),
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
                MetadataOwner = GeometrySnapshotSchema.Metadata.BoxGeometryOwner,
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
                _ => GeometrySourceKindNames.Resolve(generatedKind)
            };
        }

        private int NextSequenceId()
        {
            _sequenceId++;
            return _sequenceId;
        }
    }
}