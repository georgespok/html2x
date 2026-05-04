using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;


/// <summary>
/// Classifies the supported inline run kinds consumed by text layout.
/// </summary>
internal enum TextRunKind
{
    Normal,
    LineBreak,
    Atomic,
    InlineObject
}
