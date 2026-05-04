using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Prepares block spacing and dimension inputs for layout consumers.
/// </summary>
internal interface IBlockGeometryMeasurer
{
    BlockMeasurementBasis Prepare(BlockBox box, float availableWidth);

    BlockMeasurementBasis PrepareAtomic(BlockBox box, float availableWidth);

    float ResolveContentHeight(
        BlockBox box,
        float resolvedContentHeight,
        float minimumContentHeight = 0f);
}
