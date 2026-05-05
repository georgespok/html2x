using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

internal sealed class InlineRunCollector(
    ComputedStyle blockStyle,
    float availableWidth,
    InlineRunFactory runFactory,
    ITextMeasurer textMeasurer,
    ILineHeightStrategy lineHeightStrategy)
{
    private readonly ComputedStyle _blockStyle = blockStyle ?? throw new ArgumentNullException(nameof(blockStyle));
    private readonly InlineRunFactory _runFactory = runFactory ?? throw new ArgumentNullException(nameof(runFactory));
    private readonly ITextMeasurer _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
    private readonly ILineHeightStrategy _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
    private readonly List<TextRunInput> _runs = [];
    private int _nextRunId = 1;

    public IReadOnlyList<TextRunInput> Runs => _runs;

    public int Count => _runs.Count;

    public TextRunKind? LastKind => _runs.Count == 0 ? null : _runs[^1].Kind;

    public bool TryAppendInlineBlockRun(InlineBox inline)
    {
        ArgumentNullException.ThrowIfNull(inline);

        var inlineLayout = _runFactory.BuildInlineBlockLayout(
            inline,
            availableWidth,
            _textMeasurer,
            _lineHeightStrategy);
        var inlineRun = _runFactory.BuildInlineBlockRun(inline, _nextRunId, inlineLayout);
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

    public bool TryAppendLineBreakRun(InlineBox inline)
    {
        return TryAppendLineBreakRun(inline, _blockStyle);
    }

    public bool TryAppendLineBreakRun(InlineBox inline, ComputedStyle blockStyle)
    {
        ArgumentNullException.ThrowIfNull(inline);
        ArgumentNullException.ThrowIfNull(blockStyle);

        var lineBreakRun = _runFactory.BuildLineBreakRunFromBlockContext(inline, blockStyle, _nextRunId);
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
        _runs.Add(_runFactory.CreateSyntheticLineBreakRun(style, _nextRunId));
        _nextRunId++;
    }

    public bool TryAppendTextRun(InlineBox inline)
    {
        ArgumentNullException.ThrowIfNull(inline);

        var textRun = _runFactory.BuildTextRun(inline, _nextRunId);
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
