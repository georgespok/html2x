namespace Html2x.LayoutEngine.Formatting;

internal interface IBlockFormattingContext
{
    BlockFormattingResult Format(BlockFormattingRequest request);
}
