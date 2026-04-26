namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Names the internal formatting context owner that should handle a display node.
/// </summary>
internal enum FormattingContextRole
{
    Block,
    Inline,
    Table,
    InlineBlock
}
