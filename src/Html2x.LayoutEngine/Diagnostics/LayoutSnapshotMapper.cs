using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Models;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Diagnostics;

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
        var sequenceId = 0;

        for (var pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
        {
            var page = layout.Pages[pageIndex];
            var fragments = MapFragments(page.Children, ref sequenceId);
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

    private static IReadOnlyList<FragmentSnapshot> MapFragments(
        IEnumerable<LayoutFragment> fragments,
        ref int sequenceId)
    {
        var snapshots = new List<FragmentSnapshot>();
        foreach (var fragment in fragments)
        {
            snapshots.Add(MapFragment(fragment, ref sequenceId));
        }

        return snapshots;
    }

    private static FragmentSnapshot MapFragment(LayoutFragment fragment, ref int sequenceId)
    {
        return fragment switch
        {
            TableFragment table => MapTable(table, ref sequenceId),
            TableRowFragment row => MapTableRow(row, ref sequenceId),
            TableCellFragment cell => MapTableCell(cell, ref sequenceId),
            LineBoxFragment line => MapLineBox(line, NextSequenceId(ref sequenceId)),
            BlockFragment block => MapBlock(block, ref sequenceId),
            ImageFragment image => MapImage(image, NextSequenceId(ref sequenceId)),
            RuleFragment rule => MapRule(rule, NextSequenceId(ref sequenceId)),
            _ => MapUnknown(fragment, NextSequenceId(ref sequenceId))
        };
    }

    private static FragmentSnapshot MapTable(TableFragment table, ref int sequenceId)
    {
        var fragmentSequenceId = NextSequenceId(ref sequenceId);
        var children = MapFragments(table.Rows, ref sequenceId);

        return CreateSnapshot(
            table,
            fragmentSequenceId,
            "table",
            children,
            displayRole: table.DisplayRole,
            formattingContext: table.FormattingContext,
            markerOffset: table.MarkerOffset,
            derivedColumnCount: table.DerivedColumnCount);
    }

    private static FragmentSnapshot MapTableRow(TableRowFragment row, ref int sequenceId)
    {
        var fragmentSequenceId = NextSequenceId(ref sequenceId);
        var children = MapFragments(row.Cells, ref sequenceId);

        return CreateSnapshot(
            row,
            fragmentSequenceId,
            "table-row",
            children,
            displayRole: row.DisplayRole,
            formattingContext: row.FormattingContext,
            markerOffset: row.MarkerOffset,
            rowIndex: row.RowIndex);
    }

    private static FragmentSnapshot MapTableCell(TableCellFragment cell, ref int sequenceId)
    {
        var fragmentSequenceId = NextSequenceId(ref sequenceId);
        var children = MapFragments(cell.Children, ref sequenceId);

        return CreateSnapshot(
            cell,
            fragmentSequenceId,
            "table-cell",
            children,
            displayRole: cell.DisplayRole,
            formattingContext: cell.FormattingContext,
            markerOffset: cell.MarkerOffset,
            columnIndex: cell.ColumnIndex,
            isHeader: cell.IsHeader);
    }

    private static FragmentSnapshot MapBlock(BlockFragment block, ref int sequenceId)
    {
        var fragmentSequenceId = NextSequenceId(ref sequenceId);
        var children = MapFragments(block.Children, ref sequenceId);

        return CreateSnapshot(
            block,
            fragmentSequenceId,
            "block",
            children,
            displayRole: block.DisplayRole,
            formattingContext: block.FormattingContext,
            markerOffset: block.MarkerOffset);
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
            text: text);
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
        FragmentDisplayRole? displayRole = null,
        FormattingContextKind? formattingContext = null,
        float? markerOffset = null,
        int? derivedColumnCount = null,
        int? rowIndex = null,
        int? columnIndex = null,
        bool? isHeader = null)
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
            Borders = style.Borders,
            DisplayRole = displayRole,
            FormattingContext = formattingContext,
            MarkerOffset = markerOffset,
            DerivedColumnCount = derivedColumnCount,
            RowIndex = rowIndex,
            ColumnIndex = columnIndex,
            IsHeader = isHeader,
            Children = children
        };
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

    private static int NextSequenceId(ref int sequenceId)
    {
        sequenceId++;
        return sequenceId;
    }
}
