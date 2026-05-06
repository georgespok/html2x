using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.Builders;

internal static class PublishedLayoutTestBuilder
{
    public static PublishedLayoutTree Tree(params PublishedBlock[] blocks) =>
        new(
            new(PaperSizes.A4, new()),
            blocks);

    public static PublishedBlock Block(
        string nodePath = "body/div",
        int sourceOrder = 0,
        RectPt? rect = null,
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
        var geometry = UsedGeometryRules.FromBorderBox(
            rect ?? new RectPt(0f, 0f, 100f, 20f),
            new(),
            new(),
            markerOffset: markerOffset.GetValueOrDefault());

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

    public static PublishedInlineFlowSegment Segment(params PublishedInlineItem[] items) =>
        new(
            [
                new(
                    0,
                    new(0f, 0f, 100f, 12f),
                    new(0f, 0f, 100f, 12f),
                    9f,
                    12f,
                    "left",
                    items)
            ],
            0f,
            12f);

    public static PublishedInlineTextItem TextItem(int order, string text) =>
        new(
            order,
            new(order * 20f, 0f, 20f, 12f),
            [CreateRun(text)],
            [new($"body/span[{order}]", "span", order)]);

    public static PublishedInlineObjectItem ObjectItem(int order, PublishedBlock content) =>
        new(
            order,
            new(order * 20f, 0f, 20f, 12f),
            content);

    private static TextRun CreateRun(string text)
    {
        var font = new FontKey("Test", FontWeight.W400, FontStyle.Normal);

        return new(
            text,
            font,
            12f,
            new(0f, 9f),
            20f,
            8f,
            3f,
            ResolvedFont: TextMeasurement.CreateFallback(font, 20f, 8f, 3f).ResolvedFont);
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