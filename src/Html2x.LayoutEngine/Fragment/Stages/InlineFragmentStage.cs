using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Html2x.Abstractions.Layout;
using Html2x.LayoutEngine.Box;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly TextRunFactory _textRunFactory;
    private const float BaselineTolerance = 0.1f;

    public InlineFragmentStage()
        : this(new TextRunFactory())
    {
    }

    public InlineFragmentStage(TextRunFactory textRunFactory)
    {
        _textRunFactory = textRunFactory ?? throw new ArgumentNullException(nameof(textRunFactory));
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

        var lookup = state.BlockBindings.ToDictionary(b => b.Source, b => b.Fragment);
        var visited = new HashSet<BlockBox>();

        foreach (var binding in state.BlockBindings)
        {
            ProcessBlock(binding.Source, binding.Fragment, lookup, visited, state.Observers);
        }

        return state;
    }

    private void ProcessBlock(BlockBox blockBox, BlockFragment fragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        ISet<BlockBox> visited,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (!visited.Add(blockBox))
        {
            return;
        }

        foreach (var child in blockBox.Children)
        {
            switch (child)
            {
                case InlineBox inline:
                    ProcessInline(inline, fragment, observers);
                    break;
                case BlockBox childBlock when lookup.TryGetValue(childBlock, out var childFragment):
                    ProcessBlock(childBlock, childFragment, lookup, visited, observers);
                    break;
            }
        }
    }

    private void ProcessInline(InlineBox inline, BlockFragment parentFragment, IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (!string.IsNullOrWhiteSpace(inline.TextContent))
        {
            var run = _textRunFactory.Create(inline);
            var baselineY = run.Origin.Y + run.Ascent;
            var height = run.Ascent + run.Descent;

            // Find the parent BlockBox to get padding
            var parentBlockBox = FindParentBlockBox(inline);
            var paddingLeft = parentBlockBox?.Padding.Left ?? 0f;
            var paddingTop = parentBlockBox?.Padding.Top ?? 0f;

            // Offset text run position by padding
            var adjustedX = run.Origin.X + paddingLeft;
            var adjustedY = run.Origin.Y + paddingTop;

            var line = new LineBoxFragment
            {
                Rect = new RectangleF(adjustedX, adjustedY, run.AdvanceWidth, height),
                BaselineY = baselineY + paddingTop,
                LineHeight = height,
                Runs = [run]
            };

            var storedLine = TryMergeWithPreviousLine(parentFragment, line) ?? line;
            if (ReferenceEquals(storedLine, line))
            {
                parentFragment.Children.Add(line);
            }

            foreach (var observer in observers)
            {
                observer.OnInlineFragmentCreated(inline, parentFragment, storedLine);
            }
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            ProcessInline(childInline, parentFragment, observers);
        }
    }

    private static LineBoxFragment? TryMergeWithPreviousLine(BlockFragment parentFragment, LineBoxFragment candidate)
    {
        if (parentFragment.Children.Count == 0)
        {
            return null;
        }

        if (parentFragment.Children[^1] is not LineBoxFragment previous)
        {
            return null;
        }

        if (Math.Abs(previous.BaselineY - candidate.BaselineY) > BaselineTolerance)
        {
            return null;
        }

        var mergedRuns = new List<TextRun>(previous.Runs.Count + candidate.Runs.Count);
        mergedRuns.AddRange(previous.Runs);
        mergedRuns.AddRange(candidate.Runs);

        var left = Math.Min(previous.Rect.X, candidate.Rect.X);
        var top = Math.Min(previous.Rect.Y, candidate.Rect.Y);
        var right = Math.Max(previous.Rect.Right, candidate.Rect.Right);
        var bottom = Math.Max(previous.Rect.Bottom, candidate.Rect.Bottom);

        var merged = new LineBoxFragment
        {
            Rect = new RectangleF(left, top, right - left, bottom - top),
            BaselineY = previous.BaselineY,
            LineHeight = Math.Max(previous.LineHeight, candidate.LineHeight),
            Runs = mergedRuns,
            Style = previous.Style ?? candidate.Style,
            ZOrder = Math.Max(previous.ZOrder, candidate.ZOrder)
        };

        parentFragment.Children[parentFragment.Children.Count - 1] = merged;
        return merged;
    }

    private static BlockBox? FindParentBlockBox(InlineBox inline)
    {
        var parent = inline.Parent;
        while (parent != null)
        {
            if (parent is BlockBox blockBox)
            {
                return blockBox;
            }
            parent = parent.Parent;
        }
        return null;
    }
}
