namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Classifies the supported inline run kinds consumed by text layout.
/// </summary>
internal enum TextRunKind
{
    Normal,
    LineBreak,
    Atomic,
    InlineBox
}
