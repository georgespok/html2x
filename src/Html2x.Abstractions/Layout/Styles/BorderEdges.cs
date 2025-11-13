namespace Html2x.Abstractions.Layout.Styles;

public sealed record BorderEdges
{
    public BorderSide? Top { get; init; }
    public BorderSide? Right { get; init; }
    public BorderSide? Bottom { get; init; }
    public BorderSide? Left { get; init; }

    public static BorderEdges None { get; } = new();

    public bool HasAny =>
        Top is not null ||
        Right is not null ||
        Bottom is not null ||
        Left is not null;

    public static BorderEdges Uniform(BorderSide? side)
        => side is null
            ? None
            : new BorderEdges
            {
                Top = side,
                Right = side,
                Bottom = side,
                Left = side
            };
}
