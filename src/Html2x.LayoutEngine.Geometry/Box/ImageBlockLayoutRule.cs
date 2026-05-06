namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Lays out image blocks as replaced block content.
/// </summary>
internal sealed class ImageBlockLayoutRule(
    BlockSizingRules sizingRules,
    ImageBlockLayoutWriter imageBlockLayoutWriter) : IBlockLayoutRule
{
    private readonly ImageBlockLayoutWriter _imageBlockLayoutWriter =
        imageBlockLayoutWriter ?? throw new ArgumentNullException(nameof(imageBlockLayoutWriter));

    private readonly BlockSizingRules
        _sizingRules = sizingRules ?? throw new ArgumentNullException(nameof(sizingRules));

    public bool CanLayout(BlockBox block) => block is ImageBox;

    public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
    {
        var image = (ImageBox)block;
        var measurement = _sizingRules.Prepare(image, request.ContentWidth);
        _imageBlockLayoutWriter.Write(image, request, measurement);
        return BlockLayoutRuleResult.ForResolvedBlock(image);
    }
}