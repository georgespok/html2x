using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

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
    bool IsLineBreak = false);

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
    ColorRgba? Color);

internal sealed record TextLayoutLine(
    IReadOnlyList<TextLayoutRun> Runs,
    float LineWidth,
    float LineHeight);

internal sealed record TextLayoutResult(
    IReadOnlyList<TextLayoutLine> Lines,
    float TotalHeight,
    float MaxLineWidth);
