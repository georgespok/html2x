using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly TextRunFactory _textRunFactory;

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

        var factory = new TextRunFactory(new FontMetricsProvider(), state.Context.TextMeasurer);
        var stage = new InlineFragmentStage(factory);

        foreach (var binding in state.BlockBindings)
        {
            stage.ProcessBlock(state, binding.Source, binding.Fragment, lookup, visited, state.Observers);
        }

        return state;
    }

    private void ProcessBlock(
        FragmentBuildState state,
        BlockBox blockBox,
        BlockFragment fragment,
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
                    ProcessInline(state, inline, fragment, blockBox, ref lineBreakPending, observers);
                    break;
                case BlockBox childBlock when lookup.TryGetValue(childBlock, out var childFragment):
                    ProcessBlock(state, childBlock, childFragment, lookup, visited, observers);
                    break;
            }
        }
    }

    private void ProcessInline(
        FragmentBuildState state,
        InlineBox inline,
        BlockFragment parentFragment,
        BlockBox blockContext,
        ref bool lineBreakPending,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (IsLineBreak(inline))
        {
            lineBreakPending = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(inline.TextContent))
        {
            var run = _textRunFactory.Create(inline, blockContext);
            var height = run.Ascent + run.Descent;

            var paddingLeft = blockContext.Padding.Left;
            var paddingTop = blockContext.Padding.Top;
            var borderLeft = blockContext.Style.Borders?.Left?.Width ?? 0f;
            var borderTop = blockContext.Style.Borders?.Top?.Width ?? 0f;
            var textAlign = blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

            // Compute the content box origin (border + padding inset).
            var contentLeft = blockContext.X + borderLeft + paddingLeft;
            var contentTop = blockContext.Y + borderTop + paddingTop;

            // Only continue the current line when the last emitted fragment is a line and no <br> is pending.
            var lastEmitted = parentFragment.Children.Count > 0 ? parentFragment.Children[^1] : null;
            var canContinueLine = !lineBreakPending && lastEmitted is LineBoxFragment;

            LineBoxFragment storedLine;

            if (!canContinueLine)
            {
                var previousLine = FindLastLine(parentFragment);
                var topY = previousLine is null ? contentTop : previousLine.Rect.Bottom;
                var baselineY = topY + run.Ascent;

                var adjustedRun = run with
                {
                    Origin = new PointF(contentLeft, baselineY)
                };

                storedLine = new LineBoxFragment
                {
                    FragmentId = state.ReserveFragmentId(),
                    PageNumber = state.PageNumber,
                    Rect = new RectangleF(contentLeft, topY, run.AdvanceWidth, height),
                    BaselineY = baselineY,
                    LineHeight = height,
                    Runs = [adjustedRun],
                    TextAlign = textAlign?.ToLowerInvariant()
                };

                parentFragment.Children.Add(storedLine);
                lineBreakPending = false;
            }
            else
            {
                var previous = (LineBoxFragment)lastEmitted!;
                var x = previous.Rect.Right;
                var baselineY = previous.BaselineY;

                var adjustedRun = run with
                {
                    Origin = new PointF(x, baselineY)
                };

                var mergedRuns = new List<TextRun>(previous.Runs.Count + 1);
                mergedRuns.AddRange(previous.Runs);
                mergedRuns.Add(adjustedRun);

                var lineHeight = Math.Max(previous.LineHeight, height);
                var left = previous.Rect.Left;
                var top = previous.Rect.Top;
                var right = x + run.AdvanceWidth;

                storedLine = new LineBoxFragment
                {
                    FragmentId = previous.FragmentId,
                    PageNumber = previous.PageNumber,
                    Rect = new RectangleF(left, top, Math.Max(0, right - left), lineHeight),
                    BaselineY = baselineY,
                    LineHeight = lineHeight,
                    Runs = mergedRuns,
                    Style = previous.Style,
                    ZOrder = previous.ZOrder
                };

                parentFragment.Children[parentFragment.Children.Count - 1] = storedLine;
            }

            foreach (var observer in observers)
            {
                observer.OnInlineFragmentCreated(inline, parentFragment, storedLine);
            }
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            ProcessInline(state, childInline, parentFragment, blockContext, ref lineBreakPending, observers);
        }
    }

    private static LineBoxFragment? FindLastLine(BlockFragment parentFragment)
    {
        if (parentFragment.Children.Count == 0)
        {
            return null;
        }

        for (var i = parentFragment.Children.Count - 1; i >= 0; i--)
        {
            if (parentFragment.Children[i] is LineBoxFragment line)
            {
                return line;
            }
        }

        return null;
    }

    private static bool IsLineBreak(InlineBox inline)
        => string.Equals(inline.Element?.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase);

}
