using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Fragments.Test.Builders;

internal static class PublishedLayoutFragmentTestBuilder
{
    public static PublishedLayoutTree Tree(params PublishedBlock[] blocks)
    {
        return new PublishedLayoutTree(
            new PublishedPage(PaperSizes.A4, new Spacing()),
            blocks);
    }

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
            baseline: null,
            markerOffset: markerOffset.GetValueOrDefault(),
            allowsOverflow: false);

        return new PublishedBlock(
            new PublishedBlockIdentity(nodePath, elementIdentity: null, sourceOrder),
            new PublishedDisplayFacts(role, formattingContext, markerOffset),
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

        return new PublishedInlineLayout(segments, totalHeight, maxLineWidth);
    }

    public static PublishedInlineFlowSegment Segment(params PublishedInlineItem[] items)
    {
        return Segment(Line(items: items));
    }

    public static PublishedInlineFlowSegment Segment(params PublishedInlineLine[] lines)
    {
        var top = lines.Select(static line => line.Rect.Top).DefaultIfEmpty(0f).Min();
        var bottom = lines.Select(static line => line.Rect.Bottom).DefaultIfEmpty(0f).Max();

        return new PublishedInlineFlowSegment(lines, top, bottom - top);
    }

    public static PublishedInlineLine Line(
        int lineIndex = 0,
        RectPt? rect = null,
        RectPt? occupiedRect = null,
        float baselineY = 9f,
        float lineHeight = 12f,
        string? textAlign = "left",
        params PublishedInlineItem[] items)
    {
        return new PublishedInlineLine(
            lineIndex,
            rect ?? new RectPt(0f, 0f, 100f, 12f),
            occupiedRect ?? new RectPt(0f, 0f, 100f, 12f),
            baselineY,
            lineHeight,
            textAlign,
            items);
    }

    public static PublishedInlineTextItem TextItem(
        int order,
        string text,
        RectPt? rect = null,
        FontKey? font = null,
        ResolvedFont? resolvedFont = null)
    {
        return new PublishedInlineTextItem(
            order,
            rect ?? new RectPt(order * 20f, 0f, 20f, 12f),
            [CreateRun(text, font, resolvedFont)],
            [new PublishedInlineSource($"body/span[{order}]", "span", order)]);
    }

    public static PublishedInlineTextItem EmptyTextItem(int order, RectPt? rect = null)
    {
        return new PublishedInlineTextItem(
            order,
            rect ?? new RectPt(order * 20f, 0f, 20f, 12f),
            [],
            [new PublishedInlineSource($"body/span[{order}]", "span", order)]);
    }

    public static PublishedInlineObjectItem ObjectItem(
        int order,
        PublishedBlock content,
        RectPt? rect = null)
    {
        return new PublishedInlineObjectItem(
            order,
            rect ?? new RectPt(order * 20f, 0f, 20f, 12f),
            content);
    }

    private static TextRun CreateRun(string text, FontKey? font, ResolvedFont? resolvedFont)
    {
        var fontKey = font ?? new FontKey("Test", FontWeight.W400, FontStyle.Normal);

        return new TextRun(
            text,
            fontKey,
            12f,
            new PointPt(0f, 9f),
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

        return new VisualStyle(
            BackgroundColor: style.BackgroundColor,
            Borders: hasBorders ? style.Borders : null,
            Color: style.Color,
            Margin: style.Margin,
            Padding: style.Padding,
            WidthPt: style.WidthPt,
            HeightPt: style.HeightPt,
            Display: style.Display);
    }
}
