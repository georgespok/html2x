namespace Html2x.RenderModel.Styles;

/// <summary>
///     Defines resolved text decoration flags carried from style into text runs.
/// </summary>
[Flags]
internal enum TextDecorations
{
    None = 0,
    Underline = 1,
    Overline = 2,
    LineThrough = 4
}
