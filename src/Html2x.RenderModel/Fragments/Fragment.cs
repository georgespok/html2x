namespace Html2x.RenderModel;

/// <summary>
/// Base renderable fragment carrying page geometry, paint metadata, and validation at immutable boundaries.
/// </summary>
public abstract class Fragment
{
    private readonly RectPt _rect;
    private readonly VisualStyle _style = new();

    protected Fragment()
    {
    }

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

    public VisualStyle Style
    {
        get => _style;
        init => _style = value ?? new VisualStyle();
    } // minimal style needed to paint this box

}
