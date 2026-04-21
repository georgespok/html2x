using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment.Stages;

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

        var segmentIndex = 0;
        var hasPendingInlineFlow = false;

        foreach (var child in DisplayNodeTraversal.EnumerateFlowChildren(blockBox))
        {
            if (TryQueueInlineFlowChild(child))
            {
                hasPendingInlineFlow = true;
                continue;
            }

            FlushPendingInlineFlow(state, blockBox, fragment, ref hasPendingInlineFlow, ref segmentIndex, observers);

            if (child is not BlockBox childBlock || !lookup.TryGetValue(childBlock, out var childFragment))
            {
                continue;
            }

            fragment.AddChild(childFragment);
            ProcessBlock(state, childBlock, childFragment, lookup, visited, observers, actualBindings);
        }

        FlushPendingInlineFlow(state, blockBox, fragment, ref hasPendingInlineFlow, ref segmentIndex, observers);
    }

    private void FlushPendingInlineFlow(
        FragmentBuildState state,
        BlockBox blockContext,
        BlockFragment parentFragment,
        ref bool hasPendingInlineFlow,
        ref int segmentIndex,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (!hasPendingInlineFlow)
        {
            return;
        }

        hasPendingInlineFlow = false;

        if (blockContext.InlineLayout is null || segmentIndex >= blockContext.InlineLayout.Segments.Count)
        {
            return;
        }

        EmitSegment(state, parentFragment, blockContext.InlineLayout.Segments[segmentIndex], observers);
        segmentIndex++;
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
            Rect = textItem.Rect,
            BaselineY = line.BaselineY,
            LineHeight = line.LineHeight,
            Runs = textItem.Runs.ToList(),
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
        return child switch
        {
            InlineBox => true,
            InlineBlockBoundaryBox => true,
            BlockBox block when IsAnonymousInlineWrapper(block) => true,
            _ => false
        };
    }

    private static bool IsAnonymousInlineWrapper(BlockBox block)
    {
        return block.IsAnonymous &&
               block.Children.Count > 0 &&
               block.Children.All(static child => child is InlineBox);
    }
}
