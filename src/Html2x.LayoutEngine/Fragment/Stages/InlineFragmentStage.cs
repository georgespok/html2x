using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly TextLayoutEngine _textLayout;
    private readonly IFontMetricsProvider _metrics;
    private readonly ILineHeightStrategy _lineHeightStrategy;

    public InlineFragmentStage()
        : this(new TextLayoutEngine(new FallbackTextMeasurer()), new FontMetricsProvider(), new DefaultLineHeightStrategy())
    {
    }

    private InlineFragmentStage(
        TextLayoutEngine textLayout,
        IFontMetricsProvider metrics,
        ILineHeightStrategy lineHeightStrategy)
    {
        _textLayout = textLayout ?? throw new ArgumentNullException(nameof(textLayout));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
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

        var metricsProvider = new FontMetricsProvider();
        var textLayout = new TextLayoutEngine(state.Context.TextMeasurer);
        var stage = new InlineFragmentStage(textLayout, metricsProvider, new DefaultLineHeightStrategy());

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

        var runs = new List<TextRunInput>();
        var runId = 1;

        foreach (var child in blockBox.Children)
        {
            switch (child)
            {
                case InlineBox inline:
                    CollectInlineRuns(inline, blockBox, runs, ref runId);
                    break;
                case BlockBox childBlock when lookup.TryGetValue(childBlock, out var childFragment):
                    ProcessBlock(state, childBlock, childFragment, lookup, visited, observers);
                    break;
            }
        }

        if (runs.Count == 0)
        {
            return;
        }

        EmitLineFragments(state, blockBox, fragment, runs, observers);
    }

    private void EmitLineFragments(
        FragmentBuildState state,
        BlockBox blockContext,
        BlockFragment parentFragment,
        IReadOnlyList<TextRunInput> runs,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        var paddingLeft = blockContext.Padding.Left;
        var paddingRight = blockContext.Padding.Right;
        var paddingTop = blockContext.Padding.Top;
        var borderLeft = blockContext.Style.Borders?.Left?.Width ?? 0f;
        var borderRight = blockContext.Style.Borders?.Right?.Width ?? 0f;
        var borderTop = blockContext.Style.Borders?.Top?.Width ?? 0f;
        var textAlign = blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        var contentLeft = blockContext.X + borderLeft + paddingLeft;
        var contentTop = blockContext.Y + borderTop + paddingTop;
        var contentWidth = blockContext.Width - paddingLeft - paddingRight - borderLeft - borderRight;
        if (contentWidth <= 0f || !float.IsFinite(contentWidth))
        {
            contentWidth = float.PositiveInfinity;
        }

        var font = _metrics.GetFontKey(blockContext.Style);
        var fontSize = _metrics.GetFontSize(blockContext.Style);
        var metrics = state.Context.TextMeasurer.GetMetrics(font, fontSize);
        var lineHeight = _lineHeightStrategy.GetLineHeight(blockContext.Style, font, fontSize, metrics);

        var layout = _textLayout.Layout(new TextLayoutInput(runs, contentWidth, lineHeight));
        var lastLine = FindLastLine(parentFragment);

        foreach (var line in layout.Lines)
        {
            var topY = lastLine is null ? contentTop : lastLine.Rect.Bottom;
            var baselineY = topY + GetBaselineAscent(line);

            var lineRuns = new List<TextRun>(line.Runs.Count);
            var currentX = contentLeft;

            foreach (var run in line.Runs)
            {
                var textRun = new TextRun(
                    run.Text,
                    run.Font,
                    run.FontSizePt,
                    new PointF(currentX, baselineY),
                    run.Width,
                    run.Ascent,
                    run.Descent,
                    TextDecorations.None,
                    run.Color);

                lineRuns.Add(textRun);
                currentX += run.Width;
            }

            var storedLine = new LineBoxFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = new RectangleF(contentLeft, topY, line.LineWidth, line.LineHeight),
                BaselineY = baselineY,
                LineHeight = line.LineHeight,
                Runs = lineRuns,
                TextAlign = textAlign?.ToLowerInvariant()
            };

            parentFragment.Children.Add(storedLine);
            lastLine = storedLine;

            foreach (var source in line.Runs.Select(r => r.Source).Distinct())
            {
                foreach (var observer in observers)
                {
                    observer.OnInlineFragmentCreated(source, parentFragment, storedLine);
                }
            }
        }
    }

    private void CollectInlineRuns(
        InlineBox inline,
        BlockBox blockContext,
        List<TextRunInput> runs,
        ref int runId)
    {
        if (IsLineBreak(inline))
        {
            var font = _metrics.GetFontKey(blockContext.Style);
            var fontSize = _metrics.GetFontSize(blockContext.Style);
            runs.Add(new TextRunInput(runId++, inline, string.Empty, font, fontSize, blockContext.Style, true));
            return;
        }

        if (!string.IsNullOrEmpty(inline.TextContent))
        {
            var font = _metrics.GetFontKey(inline.Style);
            var fontSize = _metrics.GetFontSize(inline.Style);
            runs.Add(new TextRunInput(runId++, inline, inline.TextContent, font, fontSize, inline.Style));
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(childInline, blockContext, runs, ref runId);
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

    private static float GetBaselineAscent(TextLayoutLine line)
    {
        if (line.Runs.Count == 0)
        {
            return 0f;
        }

        var ascent = 0f;
        foreach (var run in line.Runs)
        {
            ascent = Math.Max(ascent, run.Ascent);
        }

        return ascent;
    }

    private static bool IsLineBreak(InlineBox inline)
        => string.Equals(inline.Element?.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase);

    private sealed class FallbackTextMeasurer : ITextMeasurer
    {
        public float MeasureWidth(FontKey font, float sizePt, string text) => text.Length;
        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) => (0f, 0f);
    }

}
