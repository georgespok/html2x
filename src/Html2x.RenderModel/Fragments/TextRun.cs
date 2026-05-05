using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;

namespace Html2x.RenderModel.Fragments;


/// <summary>
/// Represents a measured run of text with absolute baseline origin and resolved font data.
/// </summary>
public sealed record TextRun(
    string Text, // exact substring for this run (post line-wrapping)
    FontKey Font,
    float FontSizePt,
    PointPt Origin, // baseline origin (absolute)
    float AdvanceWidth, // measured width used during line-breaking
    float Ascent, // font metrics for baseline alignment
    float Descent,
    TextDecorations Decorations = TextDecorations.None,
    ColorRgba? Color = null,
    ResolvedFont? ResolvedFont = null)
{
    private readonly string _text = Text ?? throw new ArgumentNullException(nameof(Text));
    private readonly FontKey _font = Font ?? throw new ArgumentNullException(nameof(Font));
    private readonly float _fontSizePt = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(FontSizePt), FontSizePt);
    private readonly PointPt _origin = FragmentGeometryGuard.RequirePoint(nameof(Origin), Origin);
    private readonly float _advanceWidth = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(AdvanceWidth), AdvanceWidth);
    private readonly float _ascent = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(Ascent), Ascent);
    private readonly float _descent = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(Descent), Descent);

    public string Text
    {
        get => _text;
        init => _text = value ?? throw new ArgumentNullException(nameof(Text));
    }

    public FontKey Font
    {
        get => _font;
        init => _font = value ?? throw new ArgumentNullException(nameof(Font));
    }

    public float FontSizePt
    {
        get => _fontSizePt;
        init => _fontSizePt = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(FontSizePt), value);
    }

    public PointPt Origin
    {
        get => _origin;
        init => _origin = FragmentGeometryGuard.RequirePoint(nameof(Origin), value);
    }

    public float AdvanceWidth
    {
        get => _advanceWidth;
        init => _advanceWidth = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(AdvanceWidth), value);
    }

    public float Ascent
    {
        get => _ascent;
        init => _ascent = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(Ascent), value);
    }

    public float Descent
    {
        get => _descent;
        init => _descent = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(Descent), value);
    }
}
