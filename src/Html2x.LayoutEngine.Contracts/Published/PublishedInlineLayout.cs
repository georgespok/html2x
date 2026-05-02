namespace Html2x.LayoutEngine.Geometry.Published;

using System.Drawing;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Models;

internal sealed record PublishedInlineLayout
{
    public PublishedInlineLayout(
        IReadOnlyList<PublishedInlineFlowSegment> segments,
        float totalHeight,
        float maxLineWidth)
    {
        Segments = PublishedLayoutGuard.CopyList(segments, nameof(segments));
        TotalHeight = PublishedLayoutGuard.RequireNonNegativeFinite(totalHeight, nameof(totalHeight));
        MaxLineWidth = PublishedLayoutGuard.RequireNonNegativeFinite(maxLineWidth, nameof(maxLineWidth));
    }

    public IReadOnlyList<PublishedInlineFlowSegment> Segments { get; }

    public float TotalHeight { get; }

    public float MaxLineWidth { get; }
}

internal sealed record PublishedInlineFlowSegment
{
    public PublishedInlineFlowSegment(
        IReadOnlyList<PublishedInlineLine> lines,
        float top,
        float height)
    {
        Lines = PublishedLayoutGuard.CopyList(lines, nameof(lines));
        Top = PublishedLayoutGuard.RequireFinite(top, nameof(top));
        Height = PublishedLayoutGuard.RequireNonNegativeFinite(height, nameof(height));
    }

    public IReadOnlyList<PublishedInlineLine> Lines { get; }

    public float Top { get; }

    public float Height { get; }
}

internal sealed record PublishedInlineLine
{
    public PublishedInlineLine(
        int lineIndex,
        RectangleF rect,
        RectangleF occupiedRect,
        float baselineY,
        float lineHeight,
        string? textAlign,
        IReadOnlyList<PublishedInlineItem> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);

        LineIndex = lineIndex;
        Rect = PublishedLayoutGuard.RequireRect(rect, nameof(rect));
        OccupiedRect = PublishedLayoutGuard.RequireRect(occupiedRect, nameof(occupiedRect));
        BaselineY = PublishedLayoutGuard.RequireFinite(baselineY, nameof(baselineY));
        LineHeight = PublishedLayoutGuard.RequireNonNegativeFinite(lineHeight, nameof(lineHeight));
        TextAlign = textAlign;
        Items = PublishedLayoutGuard.CopyList(items, nameof(items));
    }

    public int LineIndex { get; }

    public RectangleF Rect { get; }

    public RectangleF OccupiedRect { get; }

    public float BaselineY { get; }

    public float LineHeight { get; }

    public string? TextAlign { get; }

    public IReadOnlyList<PublishedInlineItem> Items { get; }
}

internal abstract record PublishedInlineItem
{
    protected PublishedInlineItem(int order, RectangleF rect)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);

        Order = order;
        Rect = PublishedLayoutGuard.RequireRect(rect, nameof(rect));
    }

    public int Order { get; }

    public RectangleF Rect { get; }
}

internal sealed record PublishedInlineTextItem : PublishedInlineItem
{
    public PublishedInlineTextItem(
        int order,
        RectangleF rect,
        IReadOnlyList<TextRun> runs,
        IReadOnlyList<PublishedInlineSource> sources)
        : base(order, rect)
    {
        Runs = PublishedLayoutGuard.CopyList(runs, nameof(runs));
        Sources = PublishedLayoutGuard.CopyList(sources, nameof(sources));
    }

    public IReadOnlyList<TextRun> Runs { get; }

    public IReadOnlyList<PublishedInlineSource> Sources { get; }
}

internal sealed record PublishedInlineObjectItem : PublishedInlineItem
{
    public PublishedInlineObjectItem(
        int order,
        RectangleF rect,
        PublishedBlock content)
        : base(order, rect)
    {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
    }

    public PublishedBlock Content { get; }
}

internal sealed record PublishedInlineSource
{
    public PublishedInlineSource(
        string nodePath,
        string? elementIdentity,
        int sourceOrder,
        GeometrySourceIdentity? sourceIdentity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodePath);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceOrder);

        NodePath = nodePath;
        ElementIdentity = string.IsNullOrWhiteSpace(elementIdentity) ? null : elementIdentity;
        SourceOrder = sourceOrder;
        SourceIdentity = sourceIdentity ?? GeometrySourceIdentity.Unspecified;
    }

    public string NodePath { get; }

    public string? ElementIdentity { get; }

    public int SourceOrder { get; }

    public GeometrySourceIdentity SourceIdentity { get; }
}
