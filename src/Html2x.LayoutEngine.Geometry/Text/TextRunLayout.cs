using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Places text runs and inline objects into ordered inline line item layouts.
/// </summary>
internal sealed class TextRunLayout(
    AtomicInlineObjectLayoutWriter inlineObjectWriter,
    InlineLineBoundsRules lineBoundsRules)
{
    private readonly AtomicInlineObjectLayoutWriter _inlineObjectWriter =
        inlineObjectWriter ?? throw new ArgumentNullException(nameof(inlineObjectWriter));

    private readonly InlineLineBoundsRules _lineBoundsRules =
        lineBoundsRules ?? throw new ArgumentNullException(nameof(lineBoundsRules));

    public IReadOnlyList<InlineLineItemLayout> Layout(
        TextLayoutLine line,
        InlineLinePlacement placement,
        Func<TextLayoutRun, IReadOnlyList<TextRunPlacement>> createTextPlacements)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(createTextPlacements);

        var context = new RunPlacementContext(
            _inlineObjectWriter,
            _lineBoundsRules,
            placement,
            line.Runs.Count);

        foreach (var run in line.Runs)
        {
            if (run.InlineObject is not null)
            {
                context.PlaceInlineObject(run);
                continue;
            }

            foreach (var textPlacement in createTextPlacements(run))
            {
                context.PlaceTextRun(run, textPlacement);
            }
        }

        return context.Finish();
    }

    /// <summary>
    ///     Tracks incremental placement state while one inline line is converted into line items.
    /// </summary>
    private sealed class RunPlacementContext
    {
        private readonly AtomicInlineObjectLayoutWriter _inlineObjectWriter;
        private readonly List<InlineLineItemLayout> _items;
        private readonly InlineLineBoundsRules _lineBoundsRules;
        private readonly InlineLinePlacement _placement;
        private readonly List<TextRun> _runs;
        private readonly List<InlineBox> _sources;
        private float _currentX;
        private int _order;

        internal RunPlacementContext(
            AtomicInlineObjectLayoutWriter inlineObjectWriter,
            InlineLineBoundsRules lineBoundsRules,
            InlineLinePlacement placement,
            int capacity)
        {
            _inlineObjectWriter = inlineObjectWriter;
            _lineBoundsRules = lineBoundsRules;
            _placement = placement;
            _items = new(capacity);
            _runs = new(capacity);
            _sources = new(capacity);
            _currentX = placement.StartX;
        }

        internal void PlaceInlineObject(TextLayoutRun run)
        {
            if (run.InlineObject is null)
            {
                return;
            }

            FlushTextItem();
            var inlineRect = _inlineObjectWriter.Write(
                run.InlineObject,
                _currentX + run.LeftSpacing,
                _placement.BaselineY);
            _items.Add(new InlineObjectItemLayout(_order++, inlineRect, run.InlineObject.ContentBox));
            _currentX += run.LeftSpacing + run.Width + run.RightSpacing;
        }

        internal void PlaceTextRun(TextLayoutRun run, TextRunPlacement placement)
        {
            _currentX += placement.LeftSpacing;
            _runs.Add(CreateTextRun(run, placement.Text, placement.Width, _currentX, _placement.BaselineY));
            _sources.Add(run.Source);
            _currentX += placement.Width + placement.RightSpacing + placement.ExtraAfter;
        }

        internal IReadOnlyList<InlineLineItemLayout> Finish()
        {
            FlushTextItem();
            return _items;
        }

        private void FlushTextItem()
        {
            if (_runs.Count == 0)
            {
                return;
            }

            var rect = _lineBoundsRules.CreateTextItemRect(
                _runs,
                _placement.ContentLeft,
                _placement.TopY,
                _placement.LineHeight);
            var itemSources = _sources
                .Distinct()
                .ToList();

            _items.Add(new InlineTextItemLayout(_order++, rect, _runs.ToList(), itemSources));
            _runs.Clear();
            _sources.Clear();
        }

        private static TextRun CreateTextRun(
            TextLayoutRun source,
            string text,
            float width,
            float originX,
            float baselineY) =>
            new(
                text,
                source.Font,
                source.FontSizePt,
                new(originX, baselineY),
                width,
                source.Ascent,
                source.Descent,
                source.Decorations,
                source.Color,
                source.ResolvedFont);
    }
}