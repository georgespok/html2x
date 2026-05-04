namespace Html2x.RenderModel;

public readonly record struct Spacing(float Top, float Right, float Bottom, float Left)
{
    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public Spacing Add(Spacing other)
        => new(Top + other.Top, Right + other.Right, Bottom + other.Bottom, Left + other.Left);

    public Spacing Safe()
        => new(SafeValue(Top), SafeValue(Right), SafeValue(Bottom), SafeValue(Left));

    public RectPt Inset(RectPt rect)
        => rect.Inset(this);

    public static Spacing FromBorderEdges(BorderEdges? edges)
        => new(
            edges?.Top?.Width ?? 0f,
            edges?.Right?.Width ?? 0f,
            edges?.Bottom?.Width ?? 0f,
            edges?.Left?.Width ?? 0f);

    private static float SafeValue(float value)
        => float.IsFinite(value) ? value : 0f;
}
