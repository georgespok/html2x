using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;

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

/// <summary>
/// Describes a measured inline object placed as one atomic text run.
/// </summary>
internal sealed record InlineObjectLayout(
    BlockBox ContentBox,
    TextLayoutResult Layout,
    float ContentWidth,
    float ContentHeight,
    float BorderBoxWidth,
    float BorderBoxHeight,
    float Baseline,
    ImageLayoutResolution? ImageResolution = null);

/// <summary>
/// Carries source text, spacing, and font data for inline measurement.
/// </summary>
internal sealed record TextRunInput(
    int RunId,
    InlineBox Source,
    string Text,
    FontKey Font,
    float FontSizePt,
    ComputedStyle Style,
    float PaddingLeft,
    float PaddingRight,
    float MarginLeft,
    float MarginRight,
    TextRunKind Kind = TextRunKind.Normal,
    InlineObjectLayout? InlineObject = null);

/// <summary>
/// Carries the inputs needed to wrap inline runs into text lines.
/// </summary>
internal sealed record TextLayoutInput(
    IReadOnlyList<TextRunInput> Runs,
    float AvailableWidth,
    float LineHeight);

/// <summary>
/// Represents one measured run after line wrapping.
/// </summary>
internal sealed record TextLayoutRun(
    InlineBox Source,
    string Text,
    FontKey Font,
    float FontSizePt,
    float Width,
    float LeftSpacing,
    float RightSpacing,
    float Ascent,
    float Descent,
    TextDecorations Decorations,
    ColorRgba? Color,
    InlineObjectLayout? InlineObject = null);

/// <summary>
/// Represents one wrapped text line and its measured dimensions.
/// </summary>
internal sealed record TextLayoutLine(
    IReadOnlyList<TextLayoutRun> Runs,
    float LineWidth,
    float LineHeight);

/// <summary>
/// Carries all wrapped lines and aggregate inline layout metrics.
/// </summary>
internal sealed record TextLayoutResult(
    IReadOnlyList<TextLayoutLine> Lines,
    float TotalHeight,
    float MaxLineWidth);
