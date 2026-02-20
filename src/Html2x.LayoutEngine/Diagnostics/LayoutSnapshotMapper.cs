using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
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
                    NodePath = BuildNodePath(unsupportedNode),
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

        foreach (var page in layout.Pages)
        {
            var fragments = MapFragments(page.Children, ref sequenceId);

            pages.Add(new LayoutPageSnapshot
            {
                PageNumber = page.PageNumber,
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
            LineBoxFragment line => MapLineBox(line, NextSequenceId(ref sequenceId)),
            BlockFragment block => MapBlock(block, ref sequenceId),
            ImageFragment image => MapImage(image, NextSequenceId(ref sequenceId)),
            RuleFragment rule => MapRule(rule, NextSequenceId(ref sequenceId)),
            _ => MapUnknown(fragment, NextSequenceId(ref sequenceId))
        };
    }

    private static FragmentSnapshot MapBlock(BlockFragment block, ref int sequenceId)
    {
        var fragmentSequenceId = NextSequenceId(ref sequenceId);
        var children = MapFragments(block.Children, ref sequenceId);

        return new FragmentSnapshot
        {
            SequenceId = fragmentSequenceId,
            Kind = "block",
            X = block.Rect.X,
            Y = block.Rect.Y,
            Size = block.Size,
            DisplayRole = block.DisplayRole,
            FormattingContext = block.FormattingContext,
            MarkerOffset = block.MarkerOffset,
            Children = children
        };
    }

    private static FragmentSnapshot MapLineBox(LineBoxFragment line, int sequenceId)
    {
        var text = line.Runs is null
            ? null
            : string.Concat(line.Runs.Select(r => r.Text));

        return new FragmentSnapshot
        {
            SequenceId = sequenceId,
            Kind = "line",
            X = line.Rect.X,
            Y = line.Rect.Y,
            Size = line.Size,
            Text = text,
            Children = []
        };
    }

    private static FragmentSnapshot MapImage(ImageFragment image, int sequenceId)
    {
        return new FragmentSnapshot
        {
            SequenceId = sequenceId,
            Kind = "image",
            X = image.Rect.X,
            Y = image.Rect.Y,
            Size = image.Size,
            ContentX = image.ContentRect.X,
            ContentY = image.ContentRect.Y,
            ContentSize = image.ContentSize,
            Borders = image.Style?.Borders,
            Children = []
        };
    }

    private static FragmentSnapshot MapRule(RuleFragment rule, int sequenceId)
    {
        return new FragmentSnapshot
        {
            SequenceId = sequenceId,
            Kind = "rule",
            X = rule.Rect.X,
            Y = rule.Rect.Y,
            Size = rule.Size,
            Children = []
        };
    }

    private static FragmentSnapshot MapUnknown(LayoutFragment fragment, int sequenceId)
    {
        return new FragmentSnapshot
        {
            SequenceId = sequenceId,
            Kind = fragment.GetType().Name.ToLowerInvariant(),
            X = fragment.Rect.X,
            Y = fragment.Rect.Y,
            Size = fragment.Size,
            Children = []
        };
    }

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

    private static string BuildNodePath(DisplayNode node)
    {
        var segments = new Stack<string>();
        DisplayNode? current = node;

        while (current is not null)
        {
            var tag = current.Element?.TagName?.ToLowerInvariant() ?? current.Role.ToString().ToLowerInvariant();
            segments.Push(tag);
            current = current.Parent;
        }

        return string.Join("/", segments);
    }

    private static int NextSequenceId(ref int sequenceId)
    {
        sequenceId++;
        return sequenceId;
    }
}
