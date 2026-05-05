using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;

namespace Html2x.LayoutEngine.Contracts.Published;

internal sealed record PublishedInlineTextItem : PublishedInlineItem
{
    public PublishedInlineTextItem(
        int order,
        RectPt rect,
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
