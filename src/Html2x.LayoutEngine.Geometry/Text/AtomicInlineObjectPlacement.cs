using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
/// Places inline objects and writes their content box geometry and nested inline layout.
/// </summary>
internal sealed class AtomicInlineObjectPlacement(
    Func<BlockBox, TextLayoutResult, float, float, float, string?, InlineFlowSegmentLayout> buildSegment,
    LayoutBoxStateWriter stateWriter)
{
    private readonly Func<BlockBox, TextLayoutResult, float, float, float, string?, InlineFlowSegmentLayout> _buildSegment = buildSegment ?? throw new ArgumentNullException(nameof(buildSegment));
    private readonly LayoutBoxStateWriter _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public RectPt Place(InlineObjectLayout inlineObject, float left, float baselineY)
    {
        ArgumentNullException.ThrowIfNull(inlineObject);

        var contentBox = inlineObject.ContentBox;
        var padding = contentBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(contentBox.Style.Borders).Safe();
        var margin = contentBox.Style.Margin.Safe();
        var top = baselineY - inlineObject.Baseline;
        var geometry = UsedGeometryCalculator.FromBorderBox(
            left,
            top,
            inlineObject.BorderBoxWidth,
            inlineObject.BorderBoxHeight,
            padding,
            border,
            baselineY,
            contentBox.MarkerOffset);
        var contentArea = UsedGeometryCalculator.ResolveContentFlowArea(geometry);
        var inlineLayout = new InlineLayoutResult(
            [
                _buildSegment(
                    contentBox,
                    inlineObject.Layout,
                    contentArea.X,
                    contentArea.Y,
                    contentArea.Width,
                    contentBox.Style.TextAlign)
            ],
            inlineObject.ContentHeight,
            inlineObject.Layout.MaxLineWidth);

        _stateWriter.ApplyInlineObjectContentLayout(
            contentBox,
            margin,
            padding,
            geometry,
            inlineLayout,
            inlineObject.ImageResolution);

        return geometry.BorderBoxRect;
    }
}
