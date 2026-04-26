using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Models;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Diagnostics;

/// <summary>
/// Projects assembled layout output into diagnostic snapshots and validates fragment structures that cross layout boundaries.
/// </summary>
public static class LayoutSnapshotMapper
{
    private static readonly HashSet<DisplayRole> UnsupportedInlineBlockRoles =
    [
        DisplayRole.Table,
        DisplayRole.TableRow,
        DisplayRole.TableCell
    ];

    public static void ValidateInlineBlockStructures(BoxTree boxTree, DiagnosticsSession? diagnosticsSession)
    {
        ArgumentNullException.ThrowIfNull(boxTree);

        foreach (var root in boxTree.Blocks)
        {
            if (TryFindUnsupportedInlineBlockStructure(root, out var unsupportedNode))
            {
                var payload = new UnsupportedStructurePayload
                {
                    NodePath = DisplayNodePathBuilder.Build(unsupportedNode),
                    StructureKind = unsupportedNode.Role.ToString(),
                    Reason = "Unsupported structure encountered inside inline-block formatting context.",
                    FormattingContext = FormattingContextKind.InlineBlock
                };

                diagnosticsSession?.Events.Add(new DiagnosticsEvent
                {
                    Type = DiagnosticsEventType.Error,
                    Name = "layout/inline-block/unsupported-structure",
                    Payload = payload
                });

                throw new InvalidOperationException(
                    $"Unsupported inline-block internal structure: {payload.StructureKind} at {payload.NodePath}.");
            }
        }
    }

    public static LayoutSnapshot From(HtmlLayout layout)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        var pages = new List<LayoutPageSnapshot>(layout.Pages.Count);
        var fragmentMapper = new FragmentSnapshotMapper();

        for (var pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
        {
            var page = layout.Pages[pageIndex];
            var fragments = fragmentMapper.MapFragments(page.Children);
            var pageNumber = page.PageNumber > 0 ? page.PageNumber : pageIndex + 1;

            pages.Add(new LayoutPageSnapshot
            {
                PageNumber = pageNumber,
                PageSize = page.PageSize,
                Margin = page.Margins,
                Fragments = fragments
            });
        }

        return new LayoutSnapshot
        {
            PageCount = layout.Pages.Count,
            Pages = pages
        };
    }

    private static FragmentSnapshot MapLineBox(LineBoxFragment line, int sequenceId)
    {
        var text = line.Runs is null
            ? null
            : string.Concat(line.Runs.Select(r => r.Text));

        return CreateSnapshot(
            line,
            sequenceId,
            "line",
            [],
            color: ResolveLineColor(line),
            text: text,
            occupiedX: line.OccupiedRect.X,
            occupiedY: line.OccupiedRect.Y,
            occupiedSize: new SizePt(line.OccupiedRect.Width, line.OccupiedRect.Height));
    }

    private static FragmentSnapshot MapImage(ImageFragment image, int sequenceId)
    {
        return CreateSnapshot(
            image,
            sequenceId,
            "image",
            [],
            contentX: image.ContentRect.X,
            contentY: image.ContentRect.Y,
            contentSize: image.ContentSize);
    }

    private static FragmentSnapshot MapRule(RuleFragment rule, int sequenceId)
    {
        return CreateSnapshot(rule, sequenceId, "rule", []);
    }

    private static FragmentSnapshot MapUnknown(LayoutFragment fragment, int sequenceId)
    {
        return CreateSnapshot(fragment, sequenceId, fragment.GetType().Name.ToLowerInvariant(), []);
    }

    private static FragmentSnapshot CreateSnapshot(
        LayoutFragment fragment,
        int sequenceId,
        string kind,
        IReadOnlyList<FragmentSnapshot> children,
        ColorRgba? color = null,
        string? text = null,
        float? contentX = null,
        float? contentY = null,
        SizePt? contentSize = null,
        float? occupiedX = null,
        float? occupiedY = null,
        SizePt? occupiedSize = null,
        FragmentDisplayRole? displayRole = null,
        FormattingContextKind? formattingContext = null,
        float? markerOffset = null,
        int? derivedColumnCount = null,
        int? rowIndex = null,
        int? columnIndex = null,
        bool? isHeader = null,
        string? metadataOwner = null,
        string? metadataConsumer = null)
    {
        var style = fragment.Style;
        return new FragmentSnapshot
        {
            SequenceId = sequenceId,
            Kind = kind,
            X = fragment.Rect.X,
            Y = fragment.Rect.Y,
            Size = fragment.Size,
            Color = color ?? style.Color,
            BackgroundColor = style.BackgroundColor,
            Margin = style.Margin,
            Padding = style.Padding,
            WidthPt = style.WidthPt,
            HeightPt = style.HeightPt,
            Display = style.Display,
            Text = text,
            ContentX = contentX,
            ContentY = contentY,
            ContentSize = contentSize,
            OccupiedX = occupiedX,
            OccupiedY = occupiedY,
            OccupiedSize = occupiedSize,
            Borders = style.Borders,
            DisplayRole = displayRole,
            FormattingContext = formattingContext,
            MarkerOffset = markerOffset,
            DerivedColumnCount = derivedColumnCount,
            RowIndex = rowIndex,
            ColumnIndex = columnIndex,
            IsHeader = isHeader,
            MetadataOwner = metadataOwner,
            MetadataConsumer = metadataConsumer,
            Children = children
        };
    }

    private static FragmentSnapshot CreateBlockLikeSnapshot(
        BlockFragment fragment,
        int sequenceId,
        string kind,
        IReadOnlyList<FragmentSnapshot> children,
        int? derivedColumnCount = null,
        int? rowIndex = null,
        int? columnIndex = null,
        bool? isHeader = null)
    {
        return CreateSnapshot(
            fragment,
            sequenceId,
            kind,
            children,
            displayRole: fragment.DisplayRole,
            formattingContext: fragment.FormattingContext,
            markerOffset: fragment.MarkerOffset,
            derivedColumnCount: derivedColumnCount,
            rowIndex: rowIndex,
            columnIndex: columnIndex,
            isHeader: isHeader,
            metadataOwner: FragmentAdapterRegistry.MetadataOwnerName,
            metadataConsumer: nameof(LayoutSnapshotMapper));
    }

    private static ColorRgba? ResolveLineColor(LineBoxFragment line)
        => line.Style?.Color ?? line.Runs.FirstOrDefault(static run => run.Color is not null)?.Color;

    private static bool TryFindUnsupportedInlineBlockStructure(DisplayNode root, out DisplayNode unsupportedNode)
    {
        var rootIsInlineBlockContext = root is BlockBox rootBlock && rootBlock.IsInlineBlockContext;
        var stack = new Stack<(DisplayNode Node, bool InInlineBlockContext)>();
        stack.Push((root, rootIsInlineBlockContext));

        while (stack.Count > 0)
        {
            var (current, inInlineBlockContext) = stack.Pop();
            if (inInlineBlockContext && UnsupportedInlineBlockRoles.Contains(current.Role))
            {
                unsupportedNode = current;
                return true;
            }

            var childInlineBlockContext = inInlineBlockContext ||
                                          (current is BlockBox block && block.IsInlineBlockContext);

            for (var i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push((current.Children[i], childInlineBlockContext));
            }
        }

        unsupportedNode = null!;
        return false;
    }

    private sealed class FragmentSnapshotMapper
    {
        private int _sequenceId;

        public IReadOnlyList<FragmentSnapshot> MapFragments(IEnumerable<LayoutFragment> fragments)
        {
            var snapshots = new List<FragmentSnapshot>();
            foreach (var fragment in fragments)
            {
                snapshots.Add(MapFragment(fragment));
            }

            return snapshots;
        }

        private FragmentSnapshot MapFragment(LayoutFragment fragment)
        {
            return fragment switch
            {
                TableFragment table => MapTable(table),
                TableRowFragment row => MapTableRow(row),
                TableCellFragment cell => MapTableCell(cell),
                LineBoxFragment line => MapLineBox(line, NextSequenceId()),
                BlockFragment block => MapBlock(block),
                ImageFragment image => MapImage(image, NextSequenceId()),
                RuleFragment rule => MapRule(rule, NextSequenceId()),
                _ => MapUnknown(fragment, NextSequenceId())
            };
        }

        private FragmentSnapshot MapTable(TableFragment table)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(table.Rows);

            return CreateBlockLikeSnapshot(
                table,
                fragmentSequenceId,
                "table",
                children,
                derivedColumnCount: table.DerivedColumnCount);
        }

        private FragmentSnapshot MapTableRow(TableRowFragment row)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(row.Cells);

            return CreateBlockLikeSnapshot(
                row,
                fragmentSequenceId,
                "table-row",
                children,
                rowIndex: row.RowIndex);
        }

        private FragmentSnapshot MapTableCell(TableCellFragment cell)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(cell.Children);

            return CreateBlockLikeSnapshot(
                cell,
                fragmentSequenceId,
                "table-cell",
                children,
                columnIndex: cell.ColumnIndex,
                isHeader: cell.IsHeader);
        }

        private FragmentSnapshot MapBlock(BlockFragment block)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(block.Children);

            return CreateBlockLikeSnapshot(
                block,
                fragmentSequenceId,
                "block",
                children);
        }

        private int NextSequenceId()
        {
            _sequenceId++;
            return _sequenceId;
        }
    }
}
