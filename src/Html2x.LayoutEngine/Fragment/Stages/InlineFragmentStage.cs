using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly TextRunFactory _textRunFactory;

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

        var factory = new TextRunFactory(new FontMetricsProvider(), state.Context.TextMeasurer);
        var stage = new InlineFragmentStage(factory);
        var textWrapper = new TextWrapper(state.Context.TextMeasurer);

        foreach (var binding in state.BlockBindings)
        {
            stage.ProcessBlock(state, binding.Source, binding.Fragment, lookup, visited, state.Observers, textWrapper);
        }

        return state;
    }

    private void ProcessBlock(
        FragmentBuildState state,
        BlockBox blockBox,
        BlockFragment fragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        ISet<BlockBox> visited,
        IReadOnlyList<IFragmentBuildObserver> observers,
        TextWrapper textWrapper)
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
                    ProcessInline(state, inline, fragment, blockBox, ref lineBreakPending, observers, textWrapper);
                    break;
                case BlockBox childBlock when lookup.TryGetValue(childBlock, out var childFragment):
                    ProcessBlock(state, childBlock, childFragment, lookup, visited, observers, textWrapper);
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
        IReadOnlyList<IFragmentBuildObserver> observers,
        TextWrapper textWrapper)
    {
        if (IsLineBreak(inline))
        {
            lineBreakPending = true;
            return;
        }

        if (!string.IsNullOrWhiteSpace(inline.TextContent))
        {
            var rawText = inline.TextContent ?? string.Empty;
            var startsWithWhitespace = rawText.Length > 0 && char.IsWhiteSpace(rawText[0]);
            var endsWithWhitespace = rawText.Length > 0 && char.IsWhiteSpace(rawText[^1]);

            var baseRun = _textRunFactory.Create(inline, blockContext);
            var paddingLeft = blockContext.Padding.Left;
            var paddingRight = blockContext.Padding.Right;
            var paddingTop = blockContext.Padding.Top;
            var borderLeft = blockContext.Style.Borders?.Left?.Width ?? 0f;
            var borderRight = blockContext.Style.Borders?.Right?.Width ?? 0f;
            var borderTop = blockContext.Style.Borders?.Top?.Width ?? 0f;
            var textAlign = blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

            // Compute the content box origin (border + padding inset).
            var contentLeft = blockContext.X + borderLeft + paddingLeft;
            var contentTop = blockContext.Y + borderTop + paddingTop;
            var contentWidth = blockContext.Width - paddingLeft - paddingRight - borderLeft - borderRight;
            if (contentWidth <= 0f || !float.IsFinite(contentWidth))
            {
                contentWidth = float.PositiveInfinity;
            }

            var segments = textWrapper.Wrap(baseRun.Text, baseRun.Font, baseRun.FontSizePt, contentWidth).ToList();

            if (segments.Count > 0)
            {
                if (startsWithWhitespace)
                {
                    segments[0] = " " + segments[0];
                }

                if (endsWithWhitespace)
                {
                    segments[^1] += " ";
                }
            }

            if (segments.Count > 0)
            {
                var lastEmitted = parentFragment.Children.Count > 0 ? parentFragment.Children[^1] : null;
                var canContinueLine = !lineBreakPending && lastEmitted is LineBoxFragment;

                if (canContinueLine && lastEmitted is LineBoxFragment previousLine)
                {
                    var currentWidth = previousLine.Rect.Right - contentLeft;
                    var remaining = contentWidth - currentWidth;
                    if (remaining > 0f)
                    {
                        var firstSegmentWidth = state.Context.TextMeasurer.MeasureWidth(
                            baseRun.Font,
                            baseRun.FontSizePt,
                            segments[0]);

                        if (firstSegmentWidth <= remaining)
                        {
                            var appended = CreateRun(
                                state.Context.TextMeasurer,
                                baseRun,
                                segments[0],
                                previousLine.Rect.Right,
                                previousLine.BaselineY);

                            var mergedRuns = new List<TextRun>(previousLine.Runs.Count + 1);
                            mergedRuns.AddRange(previousLine.Runs);
                            mergedRuns.Add(appended);

                            var lineHeight = Math.Max(previousLine.LineHeight, appended.Ascent + appended.Descent);
                            var updatedLine = new LineBoxFragment
                            {
                                FragmentId = previousLine.FragmentId,
                                PageNumber = previousLine.PageNumber,
                                Rect = new RectangleF(
                                    previousLine.Rect.Left,
                                    previousLine.Rect.Top,
                                    Math.Max(0, previousLine.Rect.Width + firstSegmentWidth),
                                    lineHeight),
                                BaselineY = previousLine.BaselineY,
                                LineHeight = lineHeight,
                                Runs = mergedRuns,
                                Style = previousLine.Style,
                                ZOrder = previousLine.ZOrder,
                                TextAlign = previousLine.TextAlign
                            };

                            parentFragment.Children[parentFragment.Children.Count - 1] = updatedLine;
                            segments.RemoveAt(0);
                        }
                    }
                }

                foreach (var segment in segments)
                {
                    var metrics = state.Context.TextMeasurer.GetMetrics(baseRun.Font, baseRun.FontSizePt);
                    var height = metrics.Ascent + metrics.Descent;

                    var lastLine = FindLastLine(parentFragment);
                    var topY = lastLine is null ? contentTop : lastLine.Rect.Bottom;
                    var baselineY = topY + metrics.Ascent;

                    var run = CreateRun(state.Context.TextMeasurer, baseRun, segment, contentLeft, baselineY);

                    var storedLine = new LineBoxFragment
                    {
                        FragmentId = state.ReserveFragmentId(),
                        PageNumber = state.PageNumber,
                        Rect = new RectangleF(contentLeft, topY, run.AdvanceWidth, height),
                        BaselineY = baselineY,
                        LineHeight = height,
                        Runs = [run],
                        TextAlign = textAlign?.ToLowerInvariant()
                    };

                    parentFragment.Children.Add(storedLine);
                    lineBreakPending = false;

                    foreach (var observer in observers)
                    {
                        observer.OnInlineFragmentCreated(inline, parentFragment, storedLine);
                    }
                }
            }
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            ProcessInline(state, childInline, parentFragment, blockContext, ref lineBreakPending, observers, textWrapper);
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

    private static TextRun CreateRun(
        ITextMeasurer measurer,
        TextRun baseRun,
        string text,
        float originX,
        float originY)
    {
        var (ascent, descent) = measurer.GetMetrics(baseRun.Font, baseRun.FontSizePt);
        var width = measurer.MeasureWidth(baseRun.Font, baseRun.FontSizePt, text);

        return baseRun with
        {
            Text = text,
            Origin = new PointF(originX, originY),
            AdvanceWidth = width,
            Ascent = ascent,
            Descent = descent
        };
    }

}
