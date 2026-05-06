using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

internal sealed class InlineRunCollector(
    ComputedStyle blockStyle,
    float availableWidth,
    InlineRunConstruction runConstruction,
    ITextMeasurer textMeasurer,
    ILineHeightStrategy lineHeightStrategy)
{
    private readonly ComputedStyle _blockStyle = blockStyle ?? throw new ArgumentNullException(nameof(blockStyle));

    private readonly ILineHeightStrategy _lineHeightStrategy =
        lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));

    private readonly InlineRunConstruction _runConstruction =
        runConstruction ?? throw new ArgumentNullException(nameof(runConstruction));

    private readonly List<TextRunInput> _runs = [];

    private readonly ITextMeasurer
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    private int _nextRunId = 1;

    public IReadOnlyList<TextRunInput> Runs => _runs;

    public int Count => _runs.Count;

    public TextRunKind? LastKind => _runs.Count == 0 ? null : _runs[^1].Kind;

    public bool TryAppendInlineBlockRun(InlineBox inline)
    {
        ArgumentNullException.ThrowIfNull(inline);

        var inlineLayout = _runConstruction.BuildInlineBlockLayout(
            inline,
            availableWidth,
            _textMeasurer,
            _lineHeightStrategy);
        var inlineRun = _runConstruction.BuildInlineBlockRun(inline, _nextRunId, inlineLayout);
        if (inlineRun is null)
        {
            return false;
        }

        _runs.Add(inlineRun);
        _nextRunId++;
        return true;
    }

    public bool TryAppendInlineBlockBoundaryRun(InlineBlockBoundaryBox boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        return TryAppendInlineBlockRun(boundary.SourceInline);
    }

    public bool TryAppendLineBreakRun(InlineBox inline) => TryAppendLineBreakRun(inline, _blockStyle);

    public bool TryAppendLineBreakRun(InlineBox inline, ComputedStyle blockStyle)
    {
        ArgumentNullException.ThrowIfNull(inline);
        ArgumentNullException.ThrowIfNull(blockStyle);

        var lineBreakRun = _runConstruction.BuildLineBreakRunFromBlockContext(inline, blockStyle, _nextRunId);
        if (lineBreakRun is null)
        {
            return false;
        }

        _runs.Add(lineBreakRun);
        _nextRunId++;
        return true;
    }

    public void AppendSyntheticLineBreakRun(ComputedStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        _runs.Add(_runConstruction.CreateSyntheticLineBreakRun(style, _nextRunId));
        _nextRunId++;
    }

    public bool TryAppendTextRun(InlineBox inline)
    {
        ArgumentNullException.ThrowIfNull(inline);

        var textRun = _runConstruction.BuildTextRun(inline, _nextRunId);
        if (textRun is null)
        {
            return false;
        }

        _runs.Add(textRun);
        _nextRunId++;
        return true;
    }

    public void RemoveLast()
    {
        if (_runs.Count > 0)
        {
            _runs.RemoveAt(_runs.Count - 1);
        }
    }

    public void TrimBoundaryLineBreaks()
    {
        while (_runs.Count > 0 && _runs[0].Kind == TextRunKind.LineBreak)
        {
            _runs.RemoveAt(0);
        }

        while (_runs.Count > 0 && _runs[^1].Kind == TextRunKind.LineBreak)
        {
            _runs.RemoveAt(_runs.Count - 1);
        }
    }
}