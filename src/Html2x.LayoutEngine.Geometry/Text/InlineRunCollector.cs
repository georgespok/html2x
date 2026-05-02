using Html2x.LayoutEngine.Models;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;

internal sealed class InlineRunCollector
{
    private readonly ComputedStyle _blockStyle;
    private readonly float _availableWidth;
    private readonly InlineRunFactory _runFactory;
    private readonly ITextMeasurer _textMeasurer;
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly List<TextRunInput> _runs = [];
    private int _nextRunId = 1;

    public InlineRunCollector(
        ComputedStyle blockStyle,
        float availableWidth,
        InlineRunFactory runFactory,
        ITextMeasurer textMeasurer,
        ILineHeightStrategy lineHeightStrategy)
    {
        _blockStyle = blockStyle ?? throw new ArgumentNullException(nameof(blockStyle));
        _availableWidth = availableWidth;
        _runFactory = runFactory ?? throw new ArgumentNullException(nameof(runFactory));
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
    }

    public IReadOnlyList<TextRunInput> Runs => _runs;

    public int Count => _runs.Count;

    public TextRunKind? LastKind => _runs.Count == 0 ? null : _runs[^1].Kind;

    public bool TryAppendInlineBlockRun(InlineBox inline)
    {
        ArgumentNullException.ThrowIfNull(inline);

        if (!_runFactory.TryBuildInlineBlockLayout(
                inline,
                _availableWidth,
                _textMeasurer,
                _lineHeightStrategy,
                out var inlineLayout) ||
            !_runFactory.TryBuildInlineBlockRun(inline, _nextRunId, inlineLayout, out var inlineRun))
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

        if (!_runFactory.TryBuildLineBreakRunFromBlockContext(inline, blockStyle, _nextRunId, out var lineBreakRun))
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

        if (!_runFactory.TryBuildTextRun(inline, _nextRunId, out var textRun))
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
