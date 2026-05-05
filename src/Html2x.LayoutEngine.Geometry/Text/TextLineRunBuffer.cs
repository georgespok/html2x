using System.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
/// Buffers one source run while text is accumulated for a line.
/// </summary>
internal sealed class TextLineRunBuffer(TextRunInput source, InlineObjectLayout? inlineObject = null)
{
    public TextRunInput Source { get; } = source;

    public StringBuilder Text { get; } = new();

    public float LeftSpacing { get; } = source.PaddingLeft + source.MarginLeft;

    public float RightSpacing { get; } = source.PaddingRight + source.MarginRight;

    public InlineObjectLayout? InlineObject { get; } = inlineObject;

    public void Append(string text)
    {
        Text.Append(text);
    }
}
