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
        var padding = blockContext.Padding.Safe();
        var border = Spacing.FromBorderEdges(blockContext.Style.Borders).Safe();
        var textAlign = blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        var contentLeft = blockContext.X + border.Left + padding.Left;
        var contentTop = blockContext.Y + border.Top + padding.Top;
        var contentWidth = blockContext.Width - padding.Horizontal - border.Horizontal;
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

        for (var lineIndex = 0; lineIndex < layout.Lines.Count; lineIndex++)
        {
            var line = layout.Lines[lineIndex];
            var topY = lastLine is null ? contentTop : lastLine.Rect.Bottom;
            var baselineY = topY + GetBaselineAscent(line);

            var lineRuns = BuildLineRuns(
                line,
                textAlign,
                contentWidth,
                contentLeft,
                baselineY,
                lineIndex,
                layout.Lines.Count,
                state.Context.TextMeasurer,
                out var lineWidthForFragment);

            var storedLine = new LineBoxFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = new RectangleF(contentLeft, topY, lineWidthForFragment, line.LineHeight),
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

    private static List<TextRun> BuildLineRuns(
        TextLayoutLine line,
        string? textAlign,
        float contentWidth,
        float contentLeft,
        float baselineY,
        int lineIndex,
        int lineCount,
        ITextMeasurer measurer,
        out float lineWidthForFragment)
    {
        var justifyExtra = ResolveJustifyExtra(textAlign, contentWidth, line.LineWidth, line, lineIndex, lineCount);
        var lineOffsetX = ResolveLineOffset(textAlign, contentWidth, line.LineWidth, line, lineIndex, lineCount);
        var isJustified = justifyExtra > 0f;

        if (isJustified && CountWhitespace(line) > 0)
        {
            var runs = BuildJustifiedRuns(line, contentLeft, baselineY, justifyExtra, measurer, out var justifiedWidth);
            lineWidthForFragment = justifiedWidth;
            return runs;
        }

        var lineRuns = new List<TextRun>(line.Runs.Count);
        var currentX = contentLeft + lineOffsetX;

        foreach (var run in line.Runs)
        {
            currentX += run.LeftSpacing;
            var textRun = new TextRun(
                run.Text,
                run.Font,
                run.FontSizePt,
                new PointF(currentX, baselineY),
                run.Width,
                run.Ascent,
                run.Descent,
                run.Decorations,
                run.Color);

            lineRuns.Add(textRun);
            currentX += run.Width + run.RightSpacing;
        }

        lineWidthForFragment = line.LineWidth;
        return lineRuns;
    }

    private static List<TextRun> BuildJustifiedRuns(
        TextLayoutLine line,
        float contentLeft,
        float baselineY,
        float extraSpace,
        ITextMeasurer measurer,
        out float lineWidthForFragment)
    {
        var spaceCount = CountWhitespace(line);
        if (spaceCount == 0)
        {
            lineWidthForFragment = line.LineWidth;
            var fallbackRuns = new List<TextRun>(line.Runs.Count);
            var fallbackX = contentLeft;

            foreach (var run in line.Runs)
            {
                fallbackX += run.LeftSpacing;
                fallbackRuns.Add(new TextRun(
                    run.Text,
                    run.Font,
                    run.FontSizePt,
                    new PointF(fallbackX, baselineY),
                    run.Width,
                    run.Ascent,
                    run.Descent,
                    run.Decorations,
                    run.Color));
                fallbackX += run.Width + run.RightSpacing;
            }

            return fallbackRuns;
        }

        var perSpaceExtra = extraSpace / spaceCount;
        var lineRuns = new List<TextRun>();
        var currentX = contentLeft;

        foreach (var run in line.Runs)
        {
            var tokens = TextTokenization.Tokenize(run.Text);
            if (tokens.Count == 0)
            {
                continue;
            }

            for (var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
            {
                var token = tokens[tokenIndex];
                var isFirstToken = tokenIndex == 0;
                var isLastToken = tokenIndex == tokens.Count - 1;
                var leftSpacing = isFirstToken ? run.LeftSpacing : 0f;
                var rightSpacing = isLastToken ? run.RightSpacing : 0f;
                var tokenWidth = measurer.MeasureWidth(run.Font, run.FontSizePt, token);
                var whitespaceCount = CountWhitespace(token);
                var tokenExtra = whitespaceCount > 0 ? whitespaceCount * perSpaceExtra : 0f;

                currentX += leftSpacing;
                var textRun = new TextRun(
                    token,
                    run.Font,
                    run.FontSizePt,
                    new PointF(currentX, baselineY),
                    tokenWidth,
                    run.Ascent,
                    run.Descent,
                    run.Decorations,
                    run.Color);

                lineRuns.Add(textRun);
                currentX += tokenWidth + rightSpacing + tokenExtra;
            }
        }

        lineWidthForFragment = line.LineWidth + extraSpace;
        return lineRuns;
    }

    private static float ResolveLineOffset(
        string? textAlign,
        float contentWidth,
        float lineWidth,
        TextLayoutLine line,
        int lineIndex,
        int lineCount)
    {
        if (!float.IsFinite(contentWidth) || contentWidth <= 0f)
        {
            return 0f;
        }

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssConstants.Defaults.TextAlign;
        var extra = Math.Max(0f, contentWidth - lineWidth);

        return align switch
        {
            "center" => extra / 2f,
            "right" => extra,
            "justify" when ShouldJustifyLine(line, lineIndex, lineCount) => 0f,
            _ => 0f
        };
    }

    private static float ResolveJustifyExtra(
        string? textAlign,
        float contentWidth,
        float lineWidth,
        TextLayoutLine line,
        int lineIndex,
        int lineCount)
    {
        if (!float.IsFinite(contentWidth) || contentWidth <= 0f)
        {
            return 0f;
        }

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssConstants.Defaults.TextAlign;
        if (!string.Equals(align, "justify", StringComparison.OrdinalIgnoreCase))
        {
            return 0f;
        }

        if (!ShouldJustifyLine(line, lineIndex, lineCount))
        {
            return 0f;
        }

        return Math.Max(0f, contentWidth - lineWidth);
    }

    private static bool ShouldJustifyLine(TextLayoutLine line, int lineIndex, int lineCount)
    {
        return lineIndex < lineCount - 1;
    }

    private static int CountWhitespace(TextLayoutLine line)
    {
        var count = 0;
        foreach (var run in line.Runs)
        {
            count += CountWhitespace(run.Text);
        }

        return count;
    }

    private static int CountWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var count = 0;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                count++;
            }
        }

        return count;
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
            runs.Add(new TextRunInput(
                runId++,
                inline,
                string.Empty,
                font,
                fontSize,
                blockContext.Style,
                PaddingLeft: 0f,
                PaddingRight: 0f,
                MarginLeft: 0f,
                MarginRight: 0f,
                IsLineBreak: true));
            return;
        }

        if (!string.IsNullOrEmpty(inline.TextContent))
        {
            var font = _metrics.GetFontKey(inline.Style);
            var fontSize = _metrics.GetFontSize(inline.Style);
            var (paddingLeft, paddingRight, marginLeft, marginRight) = GetInlineSpacing(inline);
            runs.Add(new TextRunInput(
                runId++,
                inline,
                inline.TextContent,
                font,
                fontSize,
                inline.Style,
                paddingLeft,
                paddingRight,
                marginLeft,
                marginRight));
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

    private static (float PaddingLeft, float PaddingRight, float MarginLeft, float MarginRight) GetInlineSpacing(InlineBox inline)
    {
        var source = inline;
        if (source.Element is null && source.Parent is InlineBox parent && parent.Element is not null)
        {
            source = parent;
        }

        if (source.Element is null)
        {
            return (0f, 0f, 0f, 0f);
        }

        var padding = source.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(source.Style.Borders).Safe();
        var margin = source.Style.Margin.Safe();

        return (padding.Left + border.Left, padding.Right + border.Right, margin.Left, margin.Right);
    }

    private sealed class FallbackTextMeasurer : ITextMeasurer
    {
        public float MeasureWidth(FontKey font, float sizePt, string text) => text.Length;
        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) => (0f, 0f);
    }

}
