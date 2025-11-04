using AngleSharp.Css.Dom;

namespace Html2x.Layout.Style;

/// <summary>
/// Converts CSS values to strongly-typed representations used by the layout pipeline.
/// </summary>
public interface ICssValueConverter
{
    string GetString(ICssStyleDeclaration styles, string property, string? fallback = null);

    string NormalizeAlign(string? value, string fallback);

    bool IsBold(string? value);

    bool IsItalic(string? value);

    bool TryGetLengthPt(string? raw, out float points);

    float GetLengthPt(ICssStyleDeclaration styles, string property, float fallback);
}
