namespace Html2x.LayoutEngine.Box;

internal sealed class TextNormalizationState
{
    /// <summary>
    /// Indicates whether a collapsible whitespace has been seen and should emit a single space
    /// before the next non-whitespace character when appropriate.
    /// </summary>
    public bool PendingSpace { get; set; }

    /// <summary>
    /// Tracks whether any non-whitespace has been emitted on the current block line. Used to trim
    /// leading collapsible whitespace.
    /// </summary>
    public bool AtLineStart { get; set; } = true;

    /// <summary>
    /// Tracks whether the block has emitted any non-whitespace text so far. Required to decide if
    /// a pending space should materialize across text runs.
    /// </summary>
    public bool HasWrittenContent { get; set; }

    public void MarkLineBreak()
    {
        PendingSpace = false;
        AtLineStart = true;
    }
}
