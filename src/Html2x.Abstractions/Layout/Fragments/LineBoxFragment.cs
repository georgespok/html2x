using System.Drawing;

namespace Html2x.Abstractions.Layout.Fragments;

/// <summary>
/// Represents one line slot of inline content, including tight occupied bounds, text runs, and line metrics.
/// </summary>
public sealed class LineBoxFragment : Fragment
{
    private bool _hasOccupiedRect;
    private readonly RectangleF _occupiedRect;

    public LineBoxFragment()
    {
    }

    public RectangleF OccupiedRect
    {
        get => _hasOccupiedRect ? _occupiedRect : Rect;
        init
        {
            FragmentGeometryGuard.GuardRect(nameof(OccupiedRect), value);
            _hasOccupiedRect = true;
            _occupiedRect = value;
        }
    }

    public float BaselineY { get; init; } // absolute baseline within the line slot

    public float LineHeight { get; init; } // computed line height

    public IReadOnlyList<TextRun> Runs { get; init; } = [];

    public string? TextAlign { get; init; }
}
