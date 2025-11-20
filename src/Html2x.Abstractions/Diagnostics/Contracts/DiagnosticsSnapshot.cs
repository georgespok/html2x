namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record DiagnosticsSnapshot
{
    public DiagnosticsSnapshot(
        int sequenceStart,
        IReadOnlyList<FragmentDiagnostics> fragments,
        IReadOnlyList<BorderDiagnostics> borders,
        IReadOnlyList<string> warnings)
    {
        SequenceStart = sequenceStart;
        Fragments = fragments ?? [];
        Borders = borders ?? [];
        Warnings = warnings ?? [];
    }

    public int SequenceStart { get; }

    public IReadOnlyList<FragmentDiagnostics> Fragments { get; }

    public IReadOnlyList<BorderDiagnostics> Borders { get; }

    public IReadOnlyList<string> Warnings { get; }
}

public sealed record FragmentDiagnostics
{
    public FragmentDiagnostics(
        int fragmentIndex,
        string type,
        string displayRole,
        int lineIndex,
        string? color = null,
        float? lineHeight = null,
        string? textAlign = null,
        float? widthPx = null,
        float? heightPx = null,
        string? sourceType = null,
        float? maxWidthPx = null)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Fragment type is required.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(displayRole))
        {
            throw new ArgumentException("Display role is required.", nameof(displayRole));
        }

        FragmentIndex = fragmentIndex;
        Type = type;
        DisplayRole = displayRole;
        LineIndex = lineIndex;
        Color = color;
        LineHeight = lineHeight;
        TextAlign = textAlign;
        WidthPx = widthPx;
        HeightPx = heightPx;
        SourceType = sourceType;
        MaxWidthPx = maxWidthPx;
    }

    public int FragmentIndex { get; }

    public string Type { get; }

    public string DisplayRole { get; }

    public int LineIndex { get; }

    public string? Color { get; }

    public float? LineHeight { get; }

    public string? TextAlign { get; }

    public float? WidthPx { get; }

    public float? HeightPx { get; }

    public string? SourceType { get; }

    public float? MaxWidthPx { get; }
}

public sealed record BorderDiagnostics
{
    public BorderDiagnostics(
        int fragmentIndex,
        BorderSideDiagnostics top,
        BorderSideDiagnostics right,
        BorderSideDiagnostics bottom,
        BorderSideDiagnostics left)
    {
        FragmentIndex = fragmentIndex;
        Top = top ?? throw new ArgumentNullException(nameof(top));
        Right = right ?? throw new ArgumentNullException(nameof(right));
        Bottom = bottom ?? throw new ArgumentNullException(nameof(bottom));
        Left = left ?? throw new ArgumentNullException(nameof(left));
    }

    public int FragmentIndex { get; }

    public BorderSideDiagnostics Top { get; }

    public BorderSideDiagnostics Right { get; }

    public BorderSideDiagnostics Bottom { get; }

    public BorderSideDiagnostics Left { get; }
}

public sealed record BorderSideDiagnostics
{
    public BorderSideDiagnostics(
        float thicknessPx,
        string colorHex,
        string style)
    {
        if (thicknessPx < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thicknessPx));
        }

        if (string.IsNullOrWhiteSpace(colorHex))
        {
            throw new ArgumentException("Color is required.", nameof(colorHex));
        }

        if (string.IsNullOrWhiteSpace(style))
        {
            throw new ArgumentException("Style is required.", nameof(style));
        }

        ThicknessPx = thicknessPx;
        ColorHex = colorHex;
        Style = style;
    }

    public float ThicknessPx { get; }

    public string ColorHex { get; }

    public string Style { get; }
}
