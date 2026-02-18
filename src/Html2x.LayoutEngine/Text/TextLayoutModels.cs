using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

internal enum TextRunKind
{
    Normal,
    LineBreak,
    Atomic,
    InlineObject
}

internal sealed record InlineObjectLayout(
    BlockBox ContentBox,
    TextLayoutResult Layout,
    float ContentWidth,
    float ContentHeight,
    float Width,
    float Height,
    float Baseline);

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

internal sealed record TextLayoutInput(
    IReadOnlyList<TextRunInput> Runs,
    float AvailableWidth,
    float LineHeight);

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

internal sealed record TextLayoutLine(
    IReadOnlyList<TextLayoutRun> Runs,
    float LineWidth,
    float LineHeight);

internal sealed record TextLayoutResult(
    IReadOnlyList<TextLayoutLine> Lines,
    float TotalHeight,
    float MaxLineWidth);
