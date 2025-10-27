namespace Html2x.Core.Layout;

// One *line box* of inline content (text runs, inline images)
public sealed class LineBoxFragment : Fragment
{
    public IReadOnlyList<TextRun> Runs { get; init; } = [];
}