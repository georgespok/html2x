using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;

namespace Html2x.LayoutEngine.Fragments.Test.Builders;

internal static class PublishedLayoutFragmentTestBuilder
{
    public static PublishedLayoutTree Tree(params PublishedBlock[] blocks) =>
        new(
            new(PaperSizes.A4, new()),
            blocks);

    public static PublishedBlock Block(
        string nodePath = "body/div",
        int sourceOrder = 0,
        RectPt? rect = null,
        RectPt? contentRect = null,
        FragmentDisplayRole role = FragmentDisplayRole.Block,
        FormattingContextKind formattingContext = FormattingContextKind.Block,
        float? markerOffset = null,
        ComputedStyle? style = null,
        PublishedInlineLayout? inlineLayout = null,
        PublishedImageFacts? image = null,
        PublishedRuleFacts? rule = null,
        PublishedTableFacts? table = null,
        IReadOnlyList<PublishedBlock>? children = null,
        IReadOnlyList<PublishedBlockFlowItem>? flow = null)
    {
        var borderRect = rect ?? new RectPt(0f, 0f, 100f, 20f);
        var geometry = new UsedGeometry(
            borderRect,
            contentRect ?? borderRect,
            null,
            markerOffset.GetValueOrDefault(),
            false);

        return new(
            new(nodePath, null, sourceOrder),
            new(role, formattingContext, markerOffset),
            ToVisualStyle(style ?? new ComputedStyle()),
            geometry,
            inlineLayout,
            image,
            rule,
            table,
            children ?? [],
            flow);
    }

    public static PublishedInlineLayout InlineLayout(params PublishedInlineFlowSegment[] segments)
    {
        var totalHeight = segments.Sum(static segment => segment.Height);
        var maxLineWidth = segments
            .SelectMany(static segment => segment.Lines)
            .Select(static line => line.Rect.Width)
            .DefaultIfEmpty(0f)
            .Max();

        return new(segments, totalHeight, maxLineWidth);
    }

    public static PublishedInlineFlowSegment Segment(params PublishedInlineItem[] items) => Segment(Line(items: items));

    public static PublishedInlineFlowSegment Segment(params PublishedInlineLine[] lines)
    {
        var top = lines.Select(static line => line.Rect.Top).DefaultIfEmpty(0f).Min();
        var bottom = lines.Select(static line => line.Rect.Bottom).DefaultIfEmpty(0f).Max();

        return new(lines, top, bottom - top);
    }

    public static PublishedInlineLine Line(
        int lineIndex = 0,
        RectPt? rect = null,
        RectPt? occupiedRect = null,
        float baselineY = 9f,
        float lineHeight = 12f,
        string? textAlign = "left",
        params PublishedInlineItem[] items) =>
        new(
            lineIndex,
            rect ?? new RectPt(0f, 0f, 100f, 12f),
            occupiedRect ?? new RectPt(0f, 0f, 100f, 12f),
            baselineY,
            lineHeight,
            textAlign,
            items);

    public static PublishedInlineTextItem TextItem(
        int order,
        string text,
        RectPt? rect = null,
        FontKey? font = null,
        ResolvedFont? resolvedFont = null) =>
        new(
            order,
            rect ?? new RectPt(order * 20f, 0f, 20f, 12f),
            [CreateRun(text, font, resolvedFont)],
            [new($"body/span[{order}]", "span", order)]);

    public static PublishedInlineTextItem EmptyTextItem(int order, RectPt? rect = null) =>
        new(
            order,
            rect ?? new RectPt(order * 20f, 0f, 20f, 12f),
            [],
            [new($"body/span[{order}]", "span", order)]);

    public static PublishedInlineObjectItem ObjectItem(
        int order,
        PublishedBlock content,
        RectPt? rect = null) =>
        new(
            order,
            rect ?? new RectPt(order * 20f, 0f, 20f, 12f),
            content);

    private static TextRun CreateRun(string text, FontKey? font, ResolvedFont? resolvedFont)
    {
        var fontKey = font ?? new FontKey("Test", FontWeight.W400, FontStyle.Normal);

        return new(
            text,
            fontKey,
            12f,
            new(0f, 9f),
            20f,
            8f,
            3f,
            ResolvedFont: resolvedFont ?? new ResolvedFont(
                fontKey.Family,
                fontKey.Weight,
                fontKey.Style,
                "test://font"));
    }

    private static VisualStyle ToVisualStyle(ComputedStyle style)
    {
        var hasBorders = style.Borders?.HasAny == true;

        return new(
            style.BackgroundColor,
            hasBorders ? style.Borders : null,
            style.Color,
            style.Margin,
            style.Padding,
            style.WidthPt,
            style.HeightPt,
            style.Display);
    }
}