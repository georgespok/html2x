using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

/// <summary>
/// Places inline objects and publishes their content box geometry and nested inline layout.
/// </summary>
internal sealed class InlineObjectPlacementBuilder
{
    private readonly Func<BlockBox, TextLayoutResult, float, float, float, string?, InlineFlowSegmentLayout> _buildSegment;

    public InlineObjectPlacementBuilder(
        Func<BlockBox, TextLayoutResult, float, float, float, string?, InlineFlowSegmentLayout> buildSegment)
    {
        _buildSegment = buildSegment ?? throw new ArgumentNullException(nameof(buildSegment));
    }

    public RectangleF Place(InlineObjectLayout inlineObject, float left, float baselineY)
    {
        ArgumentNullException.ThrowIfNull(inlineObject);

        var contentBox = inlineObject.ContentBox;
        var padding = contentBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(contentBox.Style.Borders).Safe();
        var margin = contentBox.Style.Margin.Safe();
        var top = baselineY - inlineObject.Baseline;
        contentBox.Padding = padding;
        contentBox.Margin = margin;
        contentBox.TextAlign = contentBox.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        var geometry = BoxGeometryFactory.FromBorderBox(
            left,
            top,
            inlineObject.BorderBoxWidth,
            inlineObject.BorderBoxHeight,
            padding,
            border,
            baselineY,
            contentBox.MarkerOffset);
        contentBox.ApplyLayoutGeometry(geometry);
        var contentArea = BoxGeometryFactory.ResolveContentArea(geometry);
        contentBox.InlineLayout = new InlineLayoutResult(
            [
                _buildSegment(
                    contentBox,
                    inlineObject.Layout,
                    contentArea.X,
                    contentArea.Y,
                    contentArea.Width,
                contentBox.TextAlign)
            ],
            inlineObject.ContentHeight,
            inlineObject.Layout.MaxLineWidth);

        return geometry.BorderBoxRect;
    }
}
