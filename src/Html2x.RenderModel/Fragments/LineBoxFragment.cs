using Html2x.RenderModel.Geometry;

namespace Html2x.RenderModel.Fragments;

/// <summary>
/// Represents one line slot of inline content, including tight occupied bounds, text runs, and line metrics.
/// </summary>
public sealed class LineBoxFragment : Fragment
{
    private readonly bool _hasOccupiedRect;
    private readonly RectPt _occupiedRect;
    private readonly float _baselineY;
    private readonly float _lineHeight;
    private IReadOnlyList<TextRun> _runs = [];

    public RectPt OccupiedRect
    {
        get => _hasOccupiedRect ? _occupiedRect : Rect;
        init
        {
            FragmentGeometryGuard.GuardRect(nameof(OccupiedRect), value);
            _hasOccupiedRect = true;
            _occupiedRect = value;
        }
    }

    public float BaselineY
    {
        get => _baselineY;
        init => _baselineY = FragmentGeometryGuard.RequireFinite(nameof(BaselineY), value);
    } // absolute baseline within the line slot

    public float LineHeight
    {
        get => _lineHeight;
        init => _lineHeight = FragmentGeometryGuard.RequireNonNegativeFinite(nameof(LineHeight), value);
    } // computed line height

    public IReadOnlyList<TextRun> Runs
    {
        get => _runs;
        init => _runs = value?.ToArray() ?? throw new ArgumentNullException(nameof(Runs));
    }

    public string? TextAlign { get; init; }
}
