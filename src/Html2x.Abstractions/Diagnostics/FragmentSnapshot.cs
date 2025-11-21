namespace Html2x.Abstractions.Diagnostics;

public sealed class FragmentSnapshot
{
    public string Kind { get; init; } = string.Empty;

    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }

    public string? Text { get; init; }

    public IReadOnlyList<FragmentSnapshot> Children { get; init; } = [];
}