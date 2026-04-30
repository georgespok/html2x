using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Measurements.Units;
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
