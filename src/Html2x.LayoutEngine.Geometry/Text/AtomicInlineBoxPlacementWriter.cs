using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Places atomic inline boxes and writes their content box geometry and nested inline layout.
/// </summary>
internal sealed class AtomicInlineBoxPlacementWriter(
    Func<BlockBox, TextLayoutResult, float, float, float, string?, InlineFlowSegmentLayout> writeSegment,
    LayoutBoxStateWriter stateWriter)
{
    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    private readonly Func<BlockBox, TextLayoutResult, float, float, float, string?, InlineFlowSegmentLayout>
        _writeSegment =
            writeSegment ?? throw new ArgumentNullException(nameof(writeSegment));

    public RectPt Write(InlineBoxLayout inlineBox, float left, float baselineY)
    {
        ArgumentNullException.ThrowIfNull(inlineBox);

        var contentBox = inlineBox.ContentBox;
        var padding = contentBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(contentBox.Style.Borders).Safe();
        var margin = contentBox.Style.Margin.Safe();
        var top = baselineY - inlineBox.Baseline;
        var geometry = UsedGeometryRules.FromBorderBox(
            left,
            top,
            inlineBox.BorderBoxWidth,
            inlineBox.BorderBoxHeight,
            padding,
            border,
            baselineY,
            contentBox.MarkerOffset);
        var contentArea = UsedGeometryRules.ResolveContentFlowArea(geometry);
        var inlineLayout = new InlineLayoutResult(
            [
                _writeSegment(
                    contentBox,
                    inlineBox.Layout,
                    contentArea.X,
                    contentArea.Y,
                    contentArea.Width,
                    contentBox.Style.TextAlign)
            ],
            inlineBox.ContentHeight,
            inlineBox.Layout.MaxLineWidth);

        _stateWriter.ApplyInlineBoxContentLayout(
            contentBox,
            margin,
            padding,
            geometry,
            inlineLayout,
            inlineBox.ImageResolution);

        return geometry.BorderBoxRect;
    }
}
