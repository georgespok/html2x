namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Lays out image blocks as replaced block content.
/// </summary>
internal sealed class ImageBlockLayoutRule(
    BoxSizingRules sizingRules,
    ImageBlockLayoutApplier imageBlockLayoutApplier) : IBlockLayoutRule
{
    private readonly BoxSizingRules _sizingRules = sizingRules ?? throw new ArgumentNullException(nameof(sizingRules));
    private readonly ImageBlockLayoutApplier _imageBlockLayoutApplier =
        imageBlockLayoutApplier ?? throw new ArgumentNullException(nameof(imageBlockLayoutApplier));

    public bool CanLayout(BlockBox block)
    {
        return block is ImageBox;
    }

    public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
    {
        var image = (ImageBox)block;
        var measurement = _sizingRules.Prepare(image, request.ContentWidth);
        _imageBlockLayoutApplier.Apply(image, request, measurement);
        return BlockLayoutRuleResult.ForResolvedBlock(image);
    }
}
