namespace Html2x.Abstractions.Layout.Fragments;

// One *line box* of inline content (text runs, inline images)
public sealed class LineBoxFragment : Fragment
{
    public float BaselineY { get; init; } // absolute baseline within the line’s Rect
    public float LineHeight { get; init; } // computed line height
    public IReadOnlyList<TextRun> Runs { get; init; } = [];
}