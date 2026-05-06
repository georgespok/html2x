using Html2x.RenderModel.Text;

namespace Html2x.Text;

/// <summary>
///     Carries measured text dimensions together with the font resolution used for measurement.
/// </summary>
public sealed record TextMeasurement(
    float Width,
    float Ascent,
    float Descent,
    ResolvedFont ResolvedFont)
{
    public static TextMeasurement CreateFallback(
        FontKey font,
        float width,
        float ascent,
        float descent)
    {
        ArgumentNullException.ThrowIfNull(font);

        var family = string.IsNullOrWhiteSpace(font.Family)
            ? "Default"
            : font.Family;

        return new(
            width,
            ascent,
            descent,
            new(
                family,
                font.Weight,
                font.Style,
                $"fallback://{family}/{font.Weight}/{font.Style}"));
    }
}