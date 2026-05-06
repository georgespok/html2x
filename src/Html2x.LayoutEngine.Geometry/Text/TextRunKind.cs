namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Classifies the supported inline run kinds consumed by text layout.
/// </summary>
internal enum TextRunKind
{
    Normal,
    LineBreak,
    Atomic,
    InlineObject
}