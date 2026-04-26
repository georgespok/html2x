using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment.Stages;

/// <summary>
/// Projects inline layout results into fragment children while preserving block bindings for nested inline objects.
/// </summary>
public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly FragmentAdapterRegistry _fragmentAdapters;

    public InlineFragmentStage()
        : this(FragmentAdapterRegistry.CreateDefault())
    {
    }

    internal InlineFragmentStage(FragmentAdapterRegistry fragmentAdapters)
    {
        _fragmentAdapters = fragmentAdapters ?? throw new ArgumentNullException(nameof(fragmentAdapters));
    }

    public FragmentBuildState Execute(FragmentBuildState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.BlockBindings.Count == 0)
        {
            return state;
        }

        var lookup = state.BlockBindings.ToDictionary(static binding => binding.Source, static binding => binding.Fragment);
        var visited = new HashSet<BlockBox>();
        var actualBindings = new List<BlockFragmentBinding>(state.BlockBindings.Count);

        foreach (var block in state.Boxes.Blocks)
        {
            if (!lookup.TryGetValue(block, out var fragment))
            {
                continue;
            }

            ProcessBlock(state, block, fragment, lookup, visited, state.Observers, actualBindings);
        }

        return state.WithBlockBindings(actualBindings);
    }

    private void ProcessBlock(
        FragmentBuildState state,
        BlockBox blockBox,
        BlockFragment fragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        ISet<BlockBox> visited,
        IReadOnlyList<IFragmentBuildObserver> observers,
        ICollection<BlockFragmentBinding> actualBindings)
    {
        if (!visited.Add(blockBox))
        {
            return;
        }

        actualBindings.Add(new BlockFragmentBinding(blockBox, fragment));

        var flowState = new InlineFragmentFlowState(SegmentIndex: 0, HasPendingInlineFlow: false);

        foreach (var child in DisplayNodeTraversal.EnumerateFlowChildren(blockBox))
        {
            if (TryQueueInlineFlowChild(child))
            {
                flowState = flowState.QueueInlineFlow();
                continue;
            }

            flowState = FlushPendingInlineFlow(state, blockBox, fragment, flowState, observers);

            if (child is not BlockBox childBlock || !lookup.TryGetValue(childBlock, out var childFragment))
            {
                continue;
            }

            fragment.AddChild(childFragment);
            ProcessBlock(state, childBlock, childFragment, lookup, visited, observers, actualBindings);
        }

        FlushPendingInlineFlow(state, blockBox, fragment, flowState, observers);
    }

    private InlineFragmentFlowState FlushPendingInlineFlow(
        FragmentBuildState state,
        BlockBox blockContext,
        BlockFragment parentFragment,
        InlineFragmentFlowState flowState,
        IReadOnlyList<IFragmentBuildObserver> observers)
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

        EmitSegment(state, parentFragment, blockContext.InlineLayout.Segments[flowState.SegmentIndex], observers);
        return nextState.AdvanceSegment();
    }

    private void EmitSegment(
        FragmentBuildState state,
        BlockFragment parentFragment,
        InlineFlowSegmentLayout segment,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        foreach (var line in segment.Lines)
        {
            foreach (var item in line.Items)
            {
                switch (item)
                {
                    case InlineTextItemLayout textItem:
                        EmitTextItem(state, parentFragment, line, textItem, observers);
                        break;
                    case InlineObjectItemLayout objectItem:
                        parentFragment.AddChild(BuildInlineObjectFragment(state, objectItem.ContentBox, observers));
                        break;
                }
            }
        }
    }

    private void EmitTextItem(
        FragmentBuildState state,
        BlockFragment parentFragment,
        InlineLineLayout line,
        InlineTextItemLayout textItem,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (textItem.Runs.Count == 0)
        {
            return;
        }

        var fragment = new LineBoxFragment
        {
            FragmentId = state.ReserveFragmentId(),
            PageNumber = state.PageNumber,
            Rect = line.Rect,
            OccupiedRect = textItem.Rect,
            BaselineY = line.BaselineY,
            LineHeight = line.LineHeight,
            Runs = ResolveTextRuns(state, textItem.Runs),
            TextAlign = line.TextAlign
        };

        parentFragment.AddChild(fragment);

        foreach (var source in textItem.Sources.Distinct())
        {
            foreach (var observer in observers)
            {
                observer.OnInlineFragmentCreated(source, parentFragment, fragment);
            }
        }
    }

    private static List<TextRun> ResolveTextRuns(FragmentBuildState state, IReadOnlyList<TextRun> runs)
    {
        var resolved = new List<TextRun>(runs.Count);
        foreach (var run in runs)
        {
            resolved.Add(run with { ResolvedFont = state.Context.FontSource.Resolve(run.Font, nameof(InlineFragmentStage)) });
        }

        return resolved;
    }

    private Abstractions.Layout.Fragments.Fragment BuildInlineObjectFragment(
        FragmentBuildState state,
        BlockBox contentBox,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (_fragmentAdapters.TryCreateSpecialFragment(contentBox, state, out var specialFragment))
        {
            foreach (var observer in observers)
            {
                observer.OnSpecialFragmentCreated(contentBox, specialFragment);
            }

            return specialFragment;
        }

        var fragment = _fragmentAdapters.CreateBlockFragment(contentBox, state);

        foreach (var observer in observers)
        {
            observer.OnBlockFragmentCreated(contentBox, fragment);
        }

        if (contentBox.InlineLayout is not null)
        {
            foreach (var segment in contentBox.InlineLayout.Segments)
            {
                EmitSegment(state, fragment, segment, observers);
            }
        }

        return fragment;
    }

    private static bool TryQueueInlineFlowChild(DisplayNode child)
    {
        return InlineFlowClassifier.IsInlineFlowMember(child);
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
