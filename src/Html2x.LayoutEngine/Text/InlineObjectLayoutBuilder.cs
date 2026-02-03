using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

internal sealed class InlineObjectLayoutBuilder(
    ITextMeasurer measurer,
    IFontMetricsProvider metrics,
    ILineHeightStrategy lineHeightStrategy)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
    private readonly IFontMetricsProvider _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILineHeightStrategy _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
    private readonly InlineRunFactory _runFactory = new(metrics);
    private readonly TextLayoutEngine _layoutEngine = new(measurer);

    public bool TryBuildInlineBlockLayout(InlineBox inline, float availableWidth, out InlineObjectLayout layout)
    {
        if (inline.Role != DisplayRole.InlineBlock)
        {
            layout = default!;
            return false;
        }

        var contentBox = inline.Children.OfType<BlockBox>().FirstOrDefault();
        if (contentBox is null)
        {
            layout = default!;
            return false;
        }

        var padding = contentBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(contentBox.Style.Borders).Safe();
        var contentAvailableWidth = ResolveContentWidth(availableWidth, padding, border);

        var lineHeight = ResolveLineHeight(contentBox);
        var runs = CollectInlineRuns(contentBox);
        var layoutResult = _layoutEngine.Layout(new TextLayoutInput(runs, contentAvailableWidth, lineHeight));

        var maxLineWidth = layoutResult.MaxLineWidth;
        var contentWidth = ResolveFinalContentWidth(contentAvailableWidth, maxLineWidth);
        var contentHeight = layoutResult.TotalHeight;

        var totalWidth = contentWidth + padding.Horizontal + border.Horizontal;
        var totalHeight = contentHeight + padding.Vertical + border.Vertical;
        var baseline = ResolveBaseline(layoutResult, padding, border, totalHeight);

        layout = new InlineObjectLayout(
            contentBox,
            layoutResult,
            contentWidth,
            contentHeight,
            totalWidth,
            totalHeight,
            baseline);
        return true;
    }

    private float ResolveLineHeight(BlockBox contentBox)
    {
        var font = _metrics.GetFontKey(contentBox.Style);
        var fontSize = _metrics.GetFontSize(contentBox.Style);
        var metrics = _measurer.GetMetrics(font, fontSize);
        return _lineHeightStrategy.GetLineHeight(contentBox.Style, font, fontSize, metrics);
    }

    private List<TextRunInput> CollectInlineRuns(BlockBox block)
    {
        var runs = new List<TextRunInput>();
        var runId = 1;
        foreach (var inline in block.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(inline, block.Style, runs, ref runId);
        }

        return runs;
    }

    private void CollectInlineRuns(
        InlineBox inline,
        ComputedStyle blockStyle,
        ICollection<TextRunInput> runs,
        ref int runId)
    {
        if (_runFactory.TryBuildLineBreakRunFromBlockContext(inline, blockStyle, runId, out var lineBreakRun))
        {
            runs.Add(lineBreakRun);
            runId++;
            return;
        }

        if (_runFactory.TryBuildTextRun(inline, runId, out var textRun))
        {
            runs.Add(textRun);
            runId++;
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(childInline, blockStyle, runs, ref runId);
        }
    }

    private static float ResolveContentWidth(float availableWidth, Spacing padding, Spacing border)
    {
        if (!float.IsFinite(availableWidth))
        {
            return float.PositiveInfinity;
        }

        return Math.Max(0f, availableWidth - padding.Horizontal - border.Horizontal);
    }

    private static float ResolveFinalContentWidth(float availableWidth, float measuredWidth)
    {
        if (!float.IsFinite(availableWidth))
        {
            return measuredWidth;
        }

        return Math.Min(availableWidth, measuredWidth);
    }

    private static float ResolveBaseline(
        TextLayoutResult layoutResult,
        Spacing padding,
        Spacing border,
        float totalHeight)
    {
        if (layoutResult.Lines.Count == 0)
        {
            return totalHeight;
        }

        var baseline = padding.Top + border.Top;
        for (var i = 0; i < layoutResult.Lines.Count - 1; i++)
        {
            baseline += layoutResult.Lines[i].LineHeight;
        }

        baseline += ResolveLineAscent(layoutResult.Lines[^1]);
        return baseline;
    }

    private static float ResolveLineAscent(TextLayoutLine line)
    {
        var ascent = 0f;
        foreach (var run in line.Runs)
        {
            ascent = Math.Max(ascent, run.Ascent);
        }

        return ascent;
    }
}
