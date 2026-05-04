using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Published;
using LayoutFragment = Html2x.RenderModel.Fragment;

namespace Html2x.LayoutEngine.Fragments;

/// <summary>
/// Projects published layout facts into renderer-visible fragments.
/// </summary>
/// <remarks>
/// Fragment projection consumes only <see cref="PublishedLayoutTree"/>. Layout may
/// mutate boxes internally, but rendering must not depend on box internals.
/// </remarks>
internal sealed class FragmentBuilder
{
    private readonly PublishedLayoutToFragmentProjector _projector;

    internal FragmentBuilder()
        : this(new PublishedLayoutToFragmentProjector())
    {
    }

    internal FragmentBuilder(PublishedLayoutToFragmentProjector projector)
    {
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
    }

    internal FragmentTree Build(PublishedLayoutTree layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var fragments = new FragmentTree();
        var pageNumber = 1;
        var nextFragmentId = 1;

        var blockBindings = CreateBlockFragments(layout, fragments, pageNumber, ref nextFragmentId);
        AppendFlowFragments(layout, blockBindings, pageNumber, ref nextFragmentId);
        AppendSpecialFragments(blockBindings, pageNumber, ref nextFragmentId);

        return fragments;
    }

    private IReadOnlyList<PublishedBlockFragmentBinding> CreateBlockFragments(
        PublishedLayoutTree layout,
        FragmentTree fragments,
        int pageNumber,
        ref int nextFragmentId)
    {
        var bindings = new List<PublishedBlockFragmentBinding>();

        foreach (var block in layout.Blocks)
        {
            var fragment = CreateBlockFragmentRecursive(block, bindings, pageNumber, ref nextFragmentId);
            fragments.Blocks.Add(fragment);
        }

        return bindings;
    }

    private BlockFragment CreateBlockFragmentRecursive(
        PublishedBlock block,
        ICollection<PublishedBlockFragmentBinding> bindings,
        int pageNumber,
        ref int nextFragmentId)
    {
        var fragment = _projector.CreateBlockFragment(block, ReserveFragmentId(ref nextFragmentId), pageNumber);

        bindings.Add(new PublishedBlockFragmentBinding(block, fragment));

        foreach (var child in block.Children)
        {
            _ = CreateBlockFragmentRecursive(child, bindings, pageNumber, ref nextFragmentId);
        }

        return fragment;
    }

    private void AppendFlowFragments(
        PublishedLayoutTree layout,
        IReadOnlyList<PublishedBlockFragmentBinding> blockBindings,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (blockBindings.Count == 0)
        {
            return;
        }

        var lookup = blockBindings.ToDictionary(
            static binding => binding.Source,
            static binding => binding.Fragment,
            ReferenceEqualityComparer<PublishedBlock>.Instance);
        var visited = new HashSet<PublishedBlock>(ReferenceEqualityComparer<PublishedBlock>.Instance);

        foreach (var block in layout.Blocks)
        {
            if (!lookup.TryGetValue(block, out var fragment))
            {
                continue;
            }

            AppendFlowFragments(
                block,
                fragment,
                lookup,
                visited,
                pageNumber,
                ref nextFragmentId);
        }
    }

    private void AppendFlowFragments(
        PublishedBlock block,
        BlockFragment fragment,
        IReadOnlyDictionary<PublishedBlock, BlockFragment> lookup,
        ISet<PublishedBlock> visited,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (!visited.Add(block))
        {
            return;
        }

        foreach (var item in block.Flow.OrderBy(static item => item.Order))
        {
            switch (item)
            {
                case PublishedInlineFlowSegmentItem inlineSegment:
                    EmitSegment(fragment, inlineSegment.Segment, pageNumber, ref nextFragmentId);
                    break;
                case PublishedChildBlockItem childBlock when lookup.TryGetValue(childBlock.Block, out var childFragment):
                    fragment.AddChild(childFragment);
                    AppendFlowFragments(
                        childBlock.Block,
                        childFragment,
                        lookup,
                        visited,
                        pageNumber,
                        ref nextFragmentId);
                    break;
            }
        }
    }

    private void AppendSpecialFragments(
        IReadOnlyList<PublishedBlockFragmentBinding> blockBindings,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (blockBindings.Count == 0)
        {
            return;
        }

        var lookup = blockBindings.ToDictionary(
            static binding => binding.Source,
            static binding => binding.Fragment,
            ReferenceEqualityComparer<PublishedBlock>.Instance);
        var visited = new HashSet<PublishedBlock>(ReferenceEqualityComparer<PublishedBlock>.Instance);

        foreach (var binding in blockBindings)
        {
            AppendSpecialFragments(
                binding.Source,
                binding.Fragment,
                lookup,
                visited,
                pageNumber,
                ref nextFragmentId);
        }
    }

    private void AppendSpecialFragments(
        PublishedBlock block,
        BlockFragment fragment,
        IReadOnlyDictionary<PublishedBlock, BlockFragment> lookup,
        ISet<PublishedBlock> visited,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (!visited.Add(block))
        {
            return;
        }

        if (HasSpecialFragment(block) &&
            _projector.TryCreateSpecialFragment(
                block,
                ReserveFragmentId(ref nextFragmentId),
                pageNumber,
                out var ownFragment))
        {
            fragment.AddChild(ownFragment);
        }

        foreach (var child in block.Children)
        {
            if (lookup.TryGetValue(child, out var childFragment))
            {
                AppendSpecialFragments(child, childFragment, lookup, visited, pageNumber, ref nextFragmentId);
            }
        }
    }

    private BlockFragment CreateInlineObjectBlockFragment(
        PublishedBlock block,
        int pageNumber,
        ref int nextFragmentId)
    {
        var fragment = _projector.CreateBlockFragment(block, ReserveFragmentId(ref nextFragmentId), pageNumber);

        if (HasSpecialFragment(block) &&
            _projector.TryCreateSpecialFragment(
                block,
                ReserveFragmentId(ref nextFragmentId),
                pageNumber,
                out var ownFragment))
        {
            fragment.AddChild(ownFragment);
        }

        foreach (var item in block.Flow.OrderBy(static item => item.Order))
        {
            switch (item)
            {
                case PublishedInlineFlowSegmentItem inlineSegment:
                    EmitSegment(fragment, inlineSegment.Segment, pageNumber, ref nextFragmentId);
                    break;
                case PublishedChildBlockItem childBlock:
                    fragment.AddChild(CreateInlineObjectFragment(
                        childBlock.Block,
                        pageNumber,
                        ref nextFragmentId));
                    break;
            }
        }

        return fragment;
    }

    private void EmitSegment(
        BlockFragment parentFragment,
        PublishedInlineFlowSegment segment,
        int pageNumber,
        ref int nextFragmentId)
    {
        foreach (var line in segment.Lines)
        {
            foreach (var item in line.Items.OrderBy(static item => item.Order))
            {
                switch (item)
                {
                    case PublishedInlineTextItem textItem:
                        EmitTextItem(parentFragment, line, textItem, pageNumber, ref nextFragmentId);
                        break;
                    case PublishedInlineObjectItem objectItem:
                        parentFragment.AddChild(
                            CreateInlineObjectFragment(objectItem.Content, pageNumber, ref nextFragmentId));
                        break;
                }
            }
        }
    }

    private void EmitTextItem(
        BlockFragment parentFragment,
        PublishedInlineLine line,
        PublishedInlineTextItem textItem,
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
            Runs = textItem.Runs.ToList(),
            TextAlign = line.TextAlign
        };

        parentFragment.AddChild(fragment);
    }

    private LayoutFragment CreateInlineObjectFragment(
        PublishedBlock content,
        int pageNumber,
        ref int nextFragmentId)
    {
        if (HasSpecialFragment(content) &&
            _projector.TryCreateSpecialFragment(
                content,
                ReserveFragmentId(ref nextFragmentId),
                pageNumber,
                out var specialFragment))
        {
            return specialFragment;
        }

        return CreateInlineObjectBlockFragment(content, pageNumber, ref nextFragmentId);
    }

    private static bool HasSpecialFragment(PublishedBlock block)
    {
        return block.Image is not null || block.Rule is not null;
    }

    private static int ReserveFragmentId(ref int nextFragmentId)
    {
        return nextFragmentId++;
    }
}
