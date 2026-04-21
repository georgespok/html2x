using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

internal sealed class InlineLayoutResultBuilder(ITextMeasurer measurer)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    public InlineFlowSegmentLayout BuildSegment(
        BlockBox blockContext,
        TextLayoutResult layout,
        float contentLeft,
        float contentTop,
        float contentWidth,
        string? textAlign)
    {
        ArgumentNullException.ThrowIfNull(blockContext);
        ArgumentNullException.ThrowIfNull(layout);

        var lines = new List<InlineLineLayout>(layout.Lines.Count);
        var nextTopY = contentTop;

        for (var lineIndex = 0; lineIndex < layout.Lines.Count; lineIndex++)
        {
            var line = layout.Lines[lineIndex];
            var topY = nextTopY;
            var baselineY = topY + GetBaselineAscent(line);
            var items = BuildLineItems(
                line,
                textAlign,
                contentWidth,
                contentLeft,
                topY,
                baselineY,
                lineIndex,
                layout.Lines.Count);
            var rect = CreateLineRect(items, contentLeft, topY, line.LineHeight);

            lines.Add(new InlineLineLayout(
                lineIndex,
                rect,
                baselineY,
                line.LineHeight,
                textAlign?.ToLowerInvariant(),
                items));

            nextTopY = topY + line.LineHeight;
        }

        return new InlineFlowSegmentLayout(lines, contentTop, Math.Max(0f, nextTopY - contentTop));
    }

    private IReadOnlyList<InlineLineItemLayout> BuildLineItems(
        TextLayoutLine line,
        string? textAlign,
        float contentWidth,
        float contentLeft,
        float topY,
        float baselineY,
        int lineIndex,
        int lineCount)
    {
        var justifyExtra = ResolveJustifyExtra(textAlign, contentWidth, line.LineWidth, line, lineIndex, lineCount);
        return justifyExtra > 0f && CountWhitespace(line) > 0
            ? BuildJustifiedLineItems(line, contentLeft, topY, baselineY, justifyExtra)
            : BuildSequentialLineItems(line, textAlign, contentWidth, contentLeft, topY, baselineY, lineIndex, lineCount);
    }

    private IReadOnlyList<InlineLineItemLayout> BuildSequentialLineItems(
        TextLayoutLine line,
        string? textAlign,
        float contentWidth,
        float contentLeft,
        float topY,
        float baselineY,
        int lineIndex,
        int lineCount)
    {
        var items = new List<InlineLineItemLayout>();
        var segmentRuns = new List<TextRun>(line.Runs.Count);
        var segmentSources = new List<InlineBox>(line.Runs.Count);
        var lineOffsetX = ResolveLineOffset(textAlign, contentWidth, line.LineWidth, line, lineIndex, lineCount);
        var currentX = contentLeft + lineOffsetX;
        var order = 0;

        foreach (var run in line.Runs)
        {
            if (run.InlineObject is not null)
            {
                FlushTextItem(items, segmentRuns, segmentSources, contentLeft, topY, line.LineHeight, ref order);
                var inlineRect = PlaceInlineObject(run.InlineObject, currentX + run.LeftSpacing, baselineY);
                items.Add(new InlineObjectItemLayout(order++, inlineRect, run.InlineObject.ContentBox));
                currentX += run.LeftSpacing + run.Width + run.RightSpacing;
                continue;
            }

            currentX += run.LeftSpacing;
            segmentRuns.Add(new TextRun(
                run.Text,
                run.Font,
                run.FontSizePt,
                new PointF(currentX, baselineY),
                run.Width,
                run.Ascent,
                run.Descent,
                run.Decorations,
                run.Color));
            segmentSources.Add(run.Source);
            currentX += run.Width + run.RightSpacing;
        }

        FlushTextItem(items, segmentRuns, segmentSources, contentLeft, topY, line.LineHeight, ref order);
        return items;
    }

    private IReadOnlyList<InlineLineItemLayout> BuildJustifiedLineItems(
        TextLayoutLine line,
        float contentLeft,
        float topY,
        float baselineY,
        float extraSpace)
    {
        var spaceCount = CountWhitespace(line);
        if (spaceCount == 0)
        {
            return BuildSequentialLineItems(line, HtmlCssConstants.CssValues.Left, float.PositiveInfinity, contentLeft, topY, baselineY, 0, 1);
        }

        var perSpaceExtra = extraSpace / spaceCount;
        var items = new List<InlineLineItemLayout>();
        var currentRuns = new List<TextRun>();
        var currentSources = new List<InlineBox>();
        var currentX = contentLeft;
        var order = 0;

        foreach (var run in line.Runs)
        {
            if (run.InlineObject is not null)
            {
                FlushTextItem(items, currentRuns, currentSources, contentLeft, topY, line.LineHeight, ref order);
                var inlineRect = PlaceInlineObject(run.InlineObject, currentX + run.LeftSpacing, baselineY);
                items.Add(new InlineObjectItemLayout(order++, inlineRect, run.InlineObject.ContentBox));
                currentX += run.LeftSpacing + run.Width + run.RightSpacing;
                continue;
            }

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
                var tokenWidth = _measurer.MeasureWidth(run.Font, run.FontSizePt, token);
                var whitespaceCount = CountWhitespace(token);
                var tokenExtra = whitespaceCount > 0 ? whitespaceCount * perSpaceExtra : 0f;

                currentX += leftSpacing;
                currentRuns.Add(new TextRun(
                    token,
                    run.Font,
                    run.FontSizePt,
                    new PointF(currentX, baselineY),
                    tokenWidth,
                    run.Ascent,
                    run.Descent,
                    run.Decorations,
                    run.Color));
                currentSources.Add(run.Source);
                currentX += tokenWidth + rightSpacing + tokenExtra;
            }
        }

        FlushTextItem(items, currentRuns, currentSources, contentLeft, topY, line.LineHeight, ref order);
        return items;
    }

    private static RectangleF CreateLineRect(
        IReadOnlyList<InlineLineItemLayout> items,
        float contentLeft,
        float topY,
        float lineHeight)
    {
        if (items.Count == 0)
        {
            return new RectangleF(contentLeft, topY, 0f, lineHeight);
        }

        var minX = items.Min(static item => item.Rect.X);
        var maxX = items.Max(static item => item.Rect.Right);
        return new RectangleF(Math.Min(contentLeft, minX), topY, Math.Max(0f, maxX - minX), lineHeight);
    }

    private static void FlushTextItem(
        ICollection<InlineLineItemLayout> items,
        List<TextRun> runs,
        List<InlineBox> sources,
        float contentLeft,
        float topY,
        float lineHeight,
        ref int order)
    {
        if (runs.Count == 0)
        {
            return;
        }

        var rect = CreateTextItemRect(runs, contentLeft, topY, lineHeight);
        var itemSources = sources
            .Distinct()
            .ToList();

        items.Add(new InlineTextItemLayout(order++, rect, runs.ToList(), itemSources));
        runs.Clear();
        sources.Clear();
    }

    private static RectangleF CreateTextItemRect(
        IReadOnlyList<TextRun> runs,
        float contentLeft,
        float topY,
        float lineHeight)
    {
        var minX = runs.Min(static run => run.Origin.X);
        var maxX = runs.Max(static run => run.Origin.X + run.AdvanceWidth);
        return new RectangleF(Math.Min(contentLeft, minX), topY, Math.Max(0f, maxX - minX), lineHeight);
    }

    private RectangleF PlaceInlineObject(InlineObjectLayout inlineObject, float left, float baselineY)
    {
        var contentBox = inlineObject.ContentBox;
        var padding = contentBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(contentBox.Style.Borders).Safe();
        var margin = contentBox.Style.Margin.Safe();
        var top = baselineY - inlineObject.Baseline;
        var borderRect = new RectangleF(left, top, inlineObject.Width, inlineObject.Height);

        contentBox.Padding = padding;
        contentBox.Margin = margin;
        contentBox.TextAlign = contentBox.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        contentBox.UsedGeometry = UsedGeometry.FromBorderBox(
            borderRect,
            padding,
            border,
            baselineY,
            contentBox.MarkerOffset);
        contentBox.InlineLayout = new InlineLayoutResult(
            [
                BuildSegment(
                    contentBox,
                    inlineObject.Layout,
                    contentBox.UsedGeometry.Value.ContentBoxRect.X,
                    contentBox.UsedGeometry.Value.ContentBoxRect.Y,
                    Math.Max(0f, inlineObject.ContentWidth),
                    contentBox.TextAlign)
            ],
            inlineObject.ContentHeight,
            inlineObject.Layout.MaxLineWidth);

        return borderRect;
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
}
