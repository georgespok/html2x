using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.RenderModel.Fragments;

/// <summary>
///     Base renderable fragment carrying page geometry, paint metadata, and validation at immutable boundaries.
/// </summary>
public abstract class Fragment
{
    private readonly RectPt _rect;

    public int FragmentId { get; init; }

    public int PageNumber { get; init; }

    public RectPt Rect
    {
        get => _rect;
        init
        {
            FragmentGeometryGuard.GuardRect(nameof(Rect), value);
            _rect = value;
        }
    } // absolute page coords (pt)

    public SizePt Size => new(Rect.Width, Rect.Height);

    public int ZOrder { get; init; } // resolved stacking/z-index

    public VisualStyle Style { get; init; } = new(); // minimal style needed to paint this box
}