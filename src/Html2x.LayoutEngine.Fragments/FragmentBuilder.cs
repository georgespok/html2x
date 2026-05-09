using Html2x.LayoutEngine.Contracts.Published;
using Html2x.RenderModel.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Fragments;

/// <summary>
///     Projects published layout facts into renderer-visible fragments.
/// </summary>
/// <remarks>
///     Fragment projection consumes only <see cref="PublishedLayoutTree" />. Layout may
///     mutate boxes internally, but rendering must not depend on box internals.
/// </remarks>
internal sealed class FragmentBuilder
{
    private readonly PublishedLayoutToFragmentProjector _projector;

    internal FragmentBuilder()
        : this(new())
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
        var context = new FragmentProjectionState(1);

        CreateBlockFragments(layout, fragments, context);
        AppendFlowFragments(layout, context);
        AppendSpecialFragments(context);

        return fragments;
    }

    private void CreateBlockFragments(
        PublishedLayoutTree layout,
        FragmentTree fragments,
        FragmentProjectionState context)
    {
        foreach (var block in layout.Blocks)
        {
            var fragment = CreateBlockFragmentRecursive(block, context);
            fragments.Blocks.Add(fragment);
        }
    }

    private BlockFragment CreateBlockFragmentRecursive(
        PublishedBlock block,
        FragmentProjectionState context)
    {
        var fragment = _projector.CreateBlockFragment(block, context.ReserveFragmentId(), context.PageNumber);

        context.BindBlock(block, fragment);

        foreach (var child in block.Children)
        {
            _ = CreateBlockFragmentRecursive(child, context);
        }

        return fragment;
    }

    private void AppendFlowFragments(
        PublishedLayoutTree layout,
        FragmentProjectionState context)
    {
        if (context.BlockBindings.Count == 0)
        {
            return;
        }

        foreach (var block in layout.Blocks)
        {
            var fragment = context.FindBlockFragment(block);
            if (fragment is null)
            {
                continue;
            }

            AppendFlowFragments(block, fragment, context);
        }
    }

    private void AppendFlowFragments(
        PublishedBlock block,
        BlockFragment fragment,
        FragmentProjectionState context)
    {
        if (!context.VisitFlowBlock(block))
        {
            return;
        }

        foreach (var item in block.Flow.OrderBy(static item => item.Order))
        {
            switch (item)
            {
                case PublishedInlineFlowSegmentItem inlineSegment:
                    EmitSegment(fragment, inlineSegment.Segment, context);
                    break;
                case PublishedChildBlockItem childBlock
                    when context.FindBlockFragment(childBlock.Block) is { } childFragment:
                    fragment.AddChild(childFragment);
                    AppendFlowFragments(
                        childBlock.Block,
                        childFragment,
                        context);
                    break;
            }
        }
    }

    private void AppendSpecialFragments(
        FragmentProjectionState context)
    {
        if (context.BlockBindings.Count == 0)
        {
            return;
        }

        foreach (var binding in context.BlockBindings)
        {
            AppendSpecialFragments(
                binding.Source,
                binding.Fragment,
                context);
        }
    }

    private void AppendSpecialFragments(
        PublishedBlock block,
        BlockFragment fragment,
        FragmentProjectionState context)
    {
        if (!context.VisitSpecialBlock(block))
        {
            return;
        }

        AppendOwnSpecialFragment(block, fragment, context);

        foreach (var child in block.Children)
        {
            var childFragment = context.FindBlockFragment(child);
            if (childFragment is not null)
            {
                AppendSpecialFragments(child, childFragment, context);
            }
        }
    }

    private BlockFragment CreateInlineObjectBlockFragment(
        PublishedBlock block,
        FragmentProjectionState context)
    {
        var fragment = _projector.CreateBlockFragment(block, context.ReserveFragmentId(), context.PageNumber);

        AppendOwnSpecialFragment(block, fragment, context);

        foreach (var item in block.Flow.OrderBy(static item => item.Order))
        {
            switch (item)
            {
                case PublishedInlineFlowSegmentItem inlineSegment:
                    EmitSegment(fragment, inlineSegment.Segment, context);
                    break;
                case PublishedChildBlockItem childBlock:
                    fragment.AddChild(CreateInlineObjectFragment(
                        childBlock.Block,
                        context));
                    break;
            }
        }

        return fragment;
    }

    private void EmitSegment(
        BlockFragment parentFragment,
        PublishedInlineFlowSegment segment,
        FragmentProjectionState context)
    {
        foreach (var line in segment.Lines)
        {
            foreach (var item in line.Items.OrderBy(static item => item.Order))
            {
                switch (item)
                {
                    case PublishedInlineTextItem textItem:
                        EmitTextItem(parentFragment, line, textItem, context);
                        break;
                    case PublishedInlineObjectItem objectItem:
                        parentFragment.AddChild(
                            CreateInlineObjectFragment(objectItem.Content, context));
                        break;
                }
            }
        }
    }

    private void EmitTextItem(
        BlockFragment parentFragment,
        PublishedInlineLine line,
        PublishedInlineTextItem textItem,
        FragmentProjectionState context)
    {
        if (textItem.Runs.Count == 0)
        {
            return;
        }

        parentFragment.AddChild(CreateLineBoxFragment(line, textItem, context));
    }

    private LineBoxFragment CreateLineBoxFragment(
        PublishedInlineLine line,
        PublishedInlineTextItem textItem,
        FragmentProjectionState context) =>
        new()
        {
            FragmentId = context.ReserveFragmentId(),
            PageNumber = context.PageNumber,
            Rect = line.Rect,
            OccupiedRect = textItem.Rect,
            BaselineY = line.BaselineY,
            LineHeight = line.LineHeight,
            Runs = textItem.Runs.ToList(),
            TextAlign = line.TextAlign
        };

    private LayoutFragment CreateInlineObjectFragment(
        PublishedBlock content,
        FragmentProjectionState context)
    {
        if (HasSpecialFragment(content) &&
            _projector.CreateSpecialFragment(
                content,
                context.ReserveFragmentId(),
                context.PageNumber) is { } specialFragment)
        {
            return specialFragment;
        }

        return CreateInlineObjectBlockFragment(content, context);
    }

    private static bool HasSpecialFragment(PublishedBlock block) => block.Image is not null || block.Rule is not null;

    private void AppendOwnSpecialFragment(
        PublishedBlock block,
        BlockFragment fragment,
        FragmentProjectionState context)
    {
        if (!HasSpecialFragment(block))
        {
            return;
        }

        var specialFragment = _projector.CreateSpecialFragment(
            block,
            context.ReserveFragmentId(),
            context.PageNumber);
        if (specialFragment is not null)
        {
            fragment.AddChild(specialFragment);
        }
    }

}
