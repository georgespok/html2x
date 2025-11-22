using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly TextRunFactory _textRunFactory;
    private const float BaselineTolerance = 0.1f;

    public InlineFragmentStage()
        : this(new TextRunFactory())
    {
    }

    private InlineFragmentStage(TextRunFactory textRunFactory)
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

        var lineBreakPending = false;

        foreach (var child in blockBox.Children)
        {
            switch (child)
            {
                case InlineBox inline:
                    ProcessInline(inline, fragment, blockBox, ref lineBreakPending, observers);
                    break;
                case BlockBox childBlock when lookup.TryGetValue(childBlock, out var childFragment):
                    ProcessBlock(childBlock, childFragment, lookup, visited, observers);
                    break;
            }
        }
    }

    private void ProcessInline(InlineBox inline, BlockFragment parentFragment, BlockBox blockContext,
        ref bool lineBreakPending, IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (IsLineBreak(inline))
        {
            lineBreakPending = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(inline.TextContent))
        {
            var run = _textRunFactory.Create(inline);
            var baselineY = run.Origin.Y + run.Ascent;
            var height = run.Ascent + run.Descent;

            var paddingLeft = blockContext.Padding.Left;
            var paddingTop = blockContext.Padding.Top;
            var textAlign = blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

            // Offset text run position by padding
            var adjustedX = run.Origin.X + paddingLeft;
            var adjustedY = run.Origin.Y + paddingTop;

            var line = new LineBoxFragment
            {
                Rect = new RectangleF(adjustedX, adjustedY, run.AdvanceWidth, height),
                BaselineY = baselineY + paddingTop,
                LineHeight = height,
                Runs = [run],
                TextAlign = textAlign?.ToLowerInvariant()
            };

            var storedLine = TryMergeWithPreviousLine(parentFragment, line, lineBreakPending) ?? line;
            lineBreakPending = false;
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
            ProcessInline(childInline, parentFragment, blockContext, ref lineBreakPending, observers);
        }
    }

    private static LineBoxFragment? TryMergeWithPreviousLine(BlockFragment parentFragment, LineBoxFragment candidate,
        bool lineBreakPending)
    {
        if (lineBreakPending || parentFragment.Children.Count == 0)
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

    private static bool IsLineBreak(InlineBox inline)
        => string.Equals(inline.Element?.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase);

}
