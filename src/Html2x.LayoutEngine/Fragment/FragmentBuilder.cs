using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Box;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.LayoutEngine.Models;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuilder
{
    private readonly IReadOnlyList<IFragmentBuildObserver> _observers;
    private readonly BoxToFragmentProjector _projector;

    public FragmentBuilder()
        : this([])
    {
    }

    public FragmentBuilder(IEnumerable<IFragmentBuildObserver> observers)
        : this(observers, new BoxToFragmentProjector())
    {
    }

    internal FragmentBuilder(
        IEnumerable<IFragmentBuildObserver> observers,
        BoxToFragmentProjector projector)
    {
        _observers = observers?.ToArray() ?? [];
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
    }

    public FragmentTree Build(BoxTree boxes, IFontSource fontSource)
    {
        ArgumentNullException.ThrowIfNull(boxes);
        ArgumentNullException.ThrowIfNull(fontSource);

        var fragments = new FragmentTree();
        var pageNumber = 1;
        var nextFragmentId = 1;

        var blockBindings = CreateBlockFragments(boxes, fragments, pageNumber, ref nextFragmentId);
        blockBindings = AppendInlineFragments(boxes, blockBindings, fontSource, pageNumber, ref nextFragmentId);
        AppendImageAndRuleFragments(blockBindings, pageNumber, ref nextFragmentId);
        ReportPaintOrder(fragments);

        return fragments;
    }

    private IReadOnlyList<BlockFragmentBinding> CreateBlockFragments(
        BoxTree boxes,
        FragmentTree fragments,
        int pageNumber,
        ref int nextFragmentId)
    {
        var bindings = new List<BlockFragmentBinding>();

        foreach (var block in boxes.Blocks)
        {
            var fragment = CreateBlockFragmentRecursive(block, bindings, pageNumber, ref nextFragmentId);
            fragments.Blocks.Add(fragment);
        }

        return bindings;
    }

    private BlockFragment CreateBlockFragmentRecursive(
        BlockBox blockBox,
        ICollection<BlockFragmentBinding> bindings,
        int pageNumber,
        ref int nextFragmentId)
    {
        var fragment = _projector.CreateBlockFragment(blockBox, ReserveFragmentId(ref nextFragmentId), pageNumber);

        bindings.Add(new BlockFragmentBinding(blockBox, fragment));
        NotifyBlockCreated(blockBox, fragment);

        foreach (var child in BoxNodeTraversal.EnumerateBlockChildren(blockBox))
        {
            if (InlineFlowClassifier.IsInlineFlowMember(child))
            {
                continue;
            }

            _ = CreateBlockFragmentRecursive(child, bindings, pageNumber, ref nextFragmentId);
        }

        return fragment;
    }

    private IReadOnlyList<BlockFragmentBinding> AppendInlineFragments(
        BoxTree boxes,
        IReadOnlyList<BlockFragmentBinding> blockBindings,
        IFontSource fontSource,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (blockBindings.Count == 0)
        {
            return blockBindings;
        }

        var lookup = blockBindings.ToDictionary(static binding => binding.Source, static binding => binding.Fragment);
        var visited = new HashSet<BlockBox>();
        var actualBindings = new List<BlockFragmentBinding>(blockBindings.Count);

        foreach (var block in boxes.Blocks)
        {
            if (!lookup.TryGetValue(block, out var fragment))
            {
                continue;
            }

            AppendInlineFragments(
                block,
                fragment,
                lookup,
                visited,
                actualBindings,
                fontSource,
                pageNumber,
                ref nextFragmentId);
        }

        return actualBindings;
    }

    private void AppendInlineFragments(
        BlockBox blockBox,
        BlockFragment fragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        ISet<BlockBox> visited,
        ICollection<BlockFragmentBinding> actualBindings,
        IFontSource fontSource,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (!visited.Add(blockBox))
        {
            return;
        }

        actualBindings.Add(new BlockFragmentBinding(blockBox, fragment));

        var flowState = new InlineFragmentFlowState(SegmentIndex: 0, HasPendingInlineFlow: false);

        foreach (var child in BoxNodeTraversal.EnumerateFlowChildren(blockBox))
        {
            if (InlineFlowClassifier.IsInlineFlowMember(child))
            {
                flowState = flowState.QueueInlineFlow();
                continue;
            }

            flowState = FlushPendingInlineFlow(blockBox, fragment, flowState, fontSource, pageNumber, ref nextFragmentId);

            if (child is not BlockBox childBlock || !lookup.TryGetValue(childBlock, out var childFragment))
            {
                continue;
            }

            fragment.AddChild(childFragment);
            AppendInlineFragments(
                childBlock,
                childFragment,
                lookup,
                visited,
                actualBindings,
                fontSource,
                pageNumber,
                ref nextFragmentId);
        }

        FlushPendingInlineFlow(blockBox, fragment, flowState, fontSource, pageNumber, ref nextFragmentId);
    }

    private InlineFragmentFlowState FlushPendingInlineFlow(
        BlockBox blockContext,
        BlockFragment parentFragment,
        InlineFragmentFlowState flowState,
        IFontSource fontSource,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (!flowState.HasPendingInlineFlow)
        {
            return flowState;
        }

        var nextState = flowState.ClearPendingInlineFlow();

        if (blockContext.InlineLayout is null || flowState.SegmentIndex >= blockContext.InlineLayout.Segments.Count)
        {
            return nextState;
        }

        EmitSegment(
            parentFragment,
            blockContext.InlineLayout.Segments[flowState.SegmentIndex],
            fontSource,
            pageNumber,
            ref nextFragmentId);
        return nextState.AdvanceSegment();
    }

    private void EmitSegment(
        BlockFragment parentFragment,
        InlineFlowSegmentLayout segment,
        IFontSource fontSource,
        int pageNumber,
        ref int nextFragmentId)
    {
        foreach (var line in segment.Lines)
        {
            foreach (var item in line.Items)
            {
                switch (item)
                {
                    case InlineTextItemLayout textItem:
                        EmitTextItem(parentFragment, line, textItem, fontSource, pageNumber, ref nextFragmentId);
                        break;
                    case InlineObjectItemLayout objectItem:
                        parentFragment.AddChild(
                            BuildInlineObjectFragment(objectItem.ContentBox, fontSource, pageNumber, ref nextFragmentId));
                        break;
                }
            }
        }
    }

    private void EmitTextItem(
        BlockFragment parentFragment,
        InlineLineLayout line,
        InlineTextItemLayout textItem,
        IFontSource fontSource,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (textItem.Runs.Count == 0)
        {
            return;
        }

        var fragment = new LineBoxFragment
        {
            FragmentId = ReserveFragmentId(ref nextFragmentId),
            PageNumber = pageNumber,
            Rect = line.Rect,
            OccupiedRect = textItem.Rect,
            BaselineY = line.BaselineY,
            LineHeight = line.LineHeight,
            Runs = ResolveTextRuns(fontSource, textItem.Runs),
            TextAlign = line.TextAlign
        };

        parentFragment.AddChild(fragment);

        foreach (var source in textItem.Sources.Distinct())
        {
            foreach (var observer in _observers)
            {
                observer.OnInlineFragmentCreated(source, parentFragment, fragment);
            }
        }
    }

    private static List<TextRun> ResolveTextRuns(IFontSource fontSource, IReadOnlyList<TextRun> runs)
    {
        var resolved = new List<TextRun>(runs.Count);
        foreach (var run in runs)
        {
            resolved.Add(run with { ResolvedFont = fontSource.Resolve(run.Font, nameof(FragmentBuilder)) });
        }

        return resolved;
    }

    private LayoutFragment BuildInlineObjectFragment(
        BlockBox contentBox,
        IFontSource fontSource,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (contentBox is RuleBox or ImageBox &&
            _projector.TryCreateSpecialFragment(
                contentBox,
                ReserveFragmentId(ref nextFragmentId),
                pageNumber,
                out var specialFragment))
        {
            NotifySpecialCreated(contentBox, specialFragment);
            return specialFragment;
        }

        var fragment = _projector.CreateBlockFragment(contentBox, ReserveFragmentId(ref nextFragmentId), pageNumber);
        NotifyBlockCreated(contentBox, fragment);

        if (contentBox.InlineLayout is not null)
        {
            foreach (var segment in contentBox.InlineLayout.Segments)
            {
                EmitSegment(fragment, segment, fontSource, pageNumber, ref nextFragmentId);
            }
        }

        return fragment;
    }

    private void AppendImageAndRuleFragments(
        IReadOnlyList<BlockFragmentBinding> blockBindings,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (blockBindings.Count == 0)
        {
            return;
        }

        var lookup = blockBindings.ToDictionary(static binding => binding.Source, static binding => binding.Fragment);
        var visited = new HashSet<BlockBox>();

        foreach (var binding in blockBindings)
        {
            AppendImageAndRuleFragments(
                binding.Source,
                binding.Fragment,
                lookup,
                visited,
                pageNumber,
                ref nextFragmentId);
        }
    }

    private void AppendImageAndRuleFragments(
        BlockBox blockBox,
        BlockFragment blockFragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        ISet<BlockBox> visited,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (!visited.Add(blockBox))
        {
            return;
        }

        if (blockBox is RuleBox or ImageBox &&
            _projector.TryCreateSpecialFragment(
                blockBox,
                ReserveFragmentId(ref nextFragmentId),
                pageNumber,
                out var ownFragment))
        {
            blockFragment.AddChild(ownFragment);
            NotifySpecialCreated(blockBox, ownFragment);
        }

        foreach (var child in blockBox.Children)
        {
            if (child is BlockBox nested && lookup.TryGetValue(nested, out var nestedFragment))
            {
                AppendImageAndRuleFragments(nested, nestedFragment, lookup, visited, pageNumber, ref nextFragmentId);
            }
        }
    }

    private void ReportPaintOrder(FragmentTree fragments)
    {
        var orderedFragments = fragments.Blocks
            .SelectMany(Flatten)
            .OrderBy(static fragment => fragment.ZOrder)
            .ToList();

        foreach (var observer in _observers)
        {
            observer.OnZOrderCompleted(orderedFragments);
        }
    }

    private static int ReserveFragmentId(ref int nextFragmentId)
    {
        return nextFragmentId++;
    }

    private static IEnumerable<LayoutFragment> Flatten(LayoutFragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        foreach (var sub in Flatten(child))
        {
            yield return sub;
        }
    }

    private void NotifyBlockCreated(BlockBox source, BlockFragment fragment)
    {
        foreach (var observer in _observers)
        {
            observer.OnBlockFragmentCreated(source, fragment);
        }
    }

    private void NotifySpecialCreated(BoxNode source, LayoutFragment fragment)
    {
        foreach (var observer in _observers)
        {
            observer.OnSpecialFragmentCreated(source, fragment);
        }
    }

    private readonly record struct InlineFragmentFlowState(
        int SegmentIndex,
        bool HasPendingInlineFlow)
    {
        public InlineFragmentFlowState QueueInlineFlow() => this with { HasPendingInlineFlow = true };

        public InlineFragmentFlowState ClearPendingInlineFlow() => this with { HasPendingInlineFlow = false };

        public InlineFragmentFlowState AdvanceSegment() => this with { SegmentIndex = SegmentIndex + 1 };
    }
}
