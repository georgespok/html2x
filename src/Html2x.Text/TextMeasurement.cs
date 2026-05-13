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
    private readonly float _ascent = RequireNonNegativeFinite(Ascent, nameof(Ascent));
    private readonly float _descent = RequireNonNegativeFinite(Descent, nameof(Descent));
    private readonly ResolvedFont _resolvedFont = RequireResolvedFont(ResolvedFont);
    private readonly float _width = RequireNonNegativeFinite(Width, nameof(Width));

    public float Width
    {
        get => _width;
        init => _width = RequireNonNegativeFinite(value, nameof(Width));
    }

    public float Ascent
    {
        get => _ascent;
        init => _ascent = RequireNonNegativeFinite(value, nameof(Ascent));
    }

    public float Descent
    {
        get => _descent;
        init => _descent = RequireNonNegativeFinite(value, nameof(Descent));
    }

    public ResolvedFont ResolvedFont
    {
        get => _resolvedFont;
        init => _resolvedFont = RequireResolvedFont(value);
    }

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

    private static float RequireNonNegativeFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Text measurement values must be finite and non-negative.");
        }

        return value;
    }

    private static ResolvedFont RequireResolvedFont(ResolvedFont? value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ResolvedFont));
        if (string.IsNullOrWhiteSpace(value.SourceId))
        {
            throw new ArgumentException(
                "Text measurement resolved font source id cannot be empty.",
                nameof(ResolvedFont));
        }

        return value;
    }
}
