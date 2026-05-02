using System.Drawing;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Models;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.Builders;

internal static class PublishedLayoutTestBuilder
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
        RectangleF? rect = null,
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
        var geometry = BoxGeometryFactory.FromBorderBox(
            rect ?? new RectangleF(0f, 0f, 100f, 20f),
            new Spacing(),
            new Spacing(),
            markerOffset: markerOffset.GetValueOrDefault());

        return new PublishedBlock(
            new PublishedBlockIdentity(nodePath, elementIdentity: null, sourceOrder),
            new PublishedDisplayFacts(role, formattingContext, markerOffset),
            style ?? new ComputedStyle(),
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
        return new PublishedInlineFlowSegment(
            [
                new PublishedInlineLine(
                    lineIndex: 0,
                    rect: new RectangleF(0f, 0f, 100f, 12f),
                    occupiedRect: new RectangleF(0f, 0f, 100f, 12f),
                    baselineY: 9f,
                    lineHeight: 12f,
                    textAlign: "left",
                    items: items)
            ],
            top: 0f,
            height: 12f);
    }

    public static PublishedInlineTextItem TextItem(int order, string text)
    {
        return new PublishedInlineTextItem(
            order,
            new RectangleF(order * 20f, 0f, 20f, 12f),
            [CreateRun(text)],
            [new PublishedInlineSource($"body/span[{order}]", "span", order)]);
    }

    public static PublishedInlineObjectItem ObjectItem(int order, PublishedBlock content)
    {
        return new PublishedInlineObjectItem(
            order,
            new RectangleF(order * 20f, 0f, 20f, 12f),
            content);
    }

    private static TextRun CreateRun(string text)
    {
        var font = new FontKey("Test", FontWeight.W400, FontStyle.Normal);

        return new TextRun(
            text,
            font,
            12f,
            new PointF(0f, 9f),
            20f,
            8f,
            3f,
            ResolvedFont: TextMeasurement.CreateFallback(font, 20f, 8f, 3f).ResolvedFont);
    }
}
