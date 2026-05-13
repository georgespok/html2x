using Html2x.LayoutEngine.Contracts.Published;
using Html2x.RenderModel.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Fragments;

/// <summary>
///     Builds renderer-visible fragments from published layout facts.
/// </summary>
/// <remarks>
///     Fragment tree building consumes only <see cref="PublishedLayoutTree" />. Layout may
///     mutate boxes internally, but rendering must not depend on box internals.
/// </remarks>
internal sealed class FragmentTreeBuilder
{
    private readonly PublishedFragmentFactory _publishedFragmentFactory;

    internal FragmentTreeBuilder()
        : this(new())
    {
    }

    internal FragmentTreeBuilder(PublishedFragmentFactory publishedFragmentFactory)
    {
        _publishedFragmentFactory = publishedFragmentFactory ?? throw new ArgumentNullException(nameof(publishedFragmentFactory));
    }

    internal FragmentTree Build(PublishedLayoutTree layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var fragments = new FragmentTree();
        var context = new FragmentBuildState(1);

        // Keep this sequence explicit: block bindings reserve deterministic ids before flow
        // and special fragments are appended in the established post-flow order.
        CreateBlockFragments(layout, fragments, context);
        AppendFlowFragments(layout, context);
        AppendSpecialFragments(context);

        return fragments;
    }

    private void CreateBlockFragments(
        PublishedLayoutTree layout,
        FragmentTree fragments,
        FragmentBuildState context)
    {
        foreach (var block in layout.Blocks)
        {
            var fragment = CreateBlockFragmentRecursive(block, context);
            fragments.Blocks.Add(fragment);
        }
    }

    private BlockFragment CreateBlockFragmentRecursive(
        PublishedBlock block,
        FragmentBuildState context)
    {
        var fragment = _publishedFragmentFactory.CreateBlockFragment(block, context.ReserveFragmentId(), context.PageNumber);

        context.BindBlock(block, fragment);

        foreach (var child in block.Children)
        {
            _ = CreateBlockFragmentRecursive(child, context);
        }

        return fragment;
    }

    private void AppendFlowFragments(
        PublishedLayoutTree layout,
        FragmentBuildState context)
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
        FragmentBuildState context)
    {
        if (!context.VisitFlowBlock(block))
        {
            return;
        }

        foreach (var item in block.Flow.OrderBy(static item => item.Order))
        {
            EmitFlowItem(
                fragment,
                item,
                context,
                childBlock =>
                {
                    var childFragment = context.FindBlockFragment(childBlock.Block);
                    if (childFragment is null)
                    {
                        return;
                    }

                    fragment.AddChild(childFragment);
                    AppendFlowFragments(
                        childBlock.Block,
                        childFragment,
                        context);
                });
        }
    }

    private void AppendSpecialFragments(
        FragmentBuildState context)
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
        FragmentBuildState context)
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
        FragmentBuildState context)
    {
        var fragment = _publishedFragmentFactory.CreateBlockFragment(block, context.ReserveFragmentId(), context.PageNumber);

        AppendOwnSpecialFragment(block, fragment, context);

        foreach (var item in block.Flow.OrderBy(static item => item.Order))
        {
            EmitFlowItem(
                fragment,
                item,
                context,
                childBlock => fragment.AddChild(CreateInlineObjectFragment(
                    childBlock.Block,
                    context)));
        }

        return fragment;
    }

    private void EmitFlowItem(
        BlockFragment fragment,
        PublishedBlockFlowItem item,
        FragmentBuildState context,
        Action<PublishedChildBlockItem> emitChildBlock)
    {
        ArgumentNullException.ThrowIfNull(emitChildBlock);

        switch (item)
        {
            case PublishedInlineFlowSegmentItem inlineSegment:
                EmitSegment(fragment, inlineSegment.Segment, context);
                break;
            case PublishedChildBlockItem childBlock:
                emitChildBlock(childBlock);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(item),
                    item.GetType().Name,
                    "Unsupported published block flow item.");
        }
    }

    private void EmitSegment(
        BlockFragment parentFragment,
        PublishedInlineFlowSegment segment,
        FragmentBuildState context)
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
        FragmentBuildState context)
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
        FragmentBuildState context) =>
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
        FragmentBuildState context)
    {
        if (HasSpecialFragment(content) &&
            _publishedFragmentFactory.CreateSpecialFragment(
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
        FragmentBuildState context)
    {
        if (!HasSpecialFragment(block))
        {
            return;
        }

        var specialFragment = _publishedFragmentFactory.CreateSpecialFragment(
            block,
            context.ReserveFragmentId(),
            context.PageNumber);
        if (specialFragment is not null)
        {
            fragment.AddChild(specialFragment);
        }
    }

}
