using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class InlineFragmentStage : IFragmentBuildStage
{
    private readonly TextLayoutEngine _textLayout;
    private readonly IFontMetricsProvider _metrics;
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly InlineRunFactory _runFactory;
    private readonly IBlockFormattingContext _blockFormattingContext;

    public InlineFragmentStage()
        : this(
            new TextLayoutEngine(new FallbackTextMeasurer()),
            new FontMetricsProvider(),
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext())
    {
    }

    private InlineFragmentStage(
        TextLayoutEngine textLayout,
        IFontMetricsProvider metrics,
        ILineHeightStrategy lineHeightStrategy,
        IBlockFormattingContext blockFormattingContext)
    {
        _textLayout = textLayout ?? throw new ArgumentNullException(nameof(textLayout));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _runFactory = new InlineRunFactory(_metrics, _blockFormattingContext);
    }

    public FragmentBuildState Execute(FragmentBuildState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.BlockBindings.Count == 0)
        {
            return state;
        }

        var lookup = state.BlockBindings.ToDictionary(b => b.Source, b => b.Fragment);
        var visited = new HashSet<BlockBox>();
        var actualBindings = new List<BlockFragmentBinding>(state.BlockBindings.Count);

        var metricsProvider = new FontMetricsProvider();
        var textLayout = new TextLayoutEngine(state.Context.TextMeasurer);
        var stage = new InlineFragmentStage(
            textLayout,
            metricsProvider,
            new DefaultLineHeightStrategy(),
            state.Context.BlockFormattingContext);

        foreach (var block in state.Boxes.Blocks)
        {
            if (!lookup.TryGetValue(block, out var fragment))
            {
                continue;
            }

            stage.ProcessBlock(state, block, fragment, lookup, visited, state.Observers, actualBindings);
        }

        return state.WithBlockBindings(actualBindings);
    }

    private void ProcessBlock(
        FragmentBuildState state,
        BlockBox blockBox,
        BlockFragment fragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        ISet<BlockBox> visited,
        IReadOnlyList<IFragmentBuildObserver> observers,
        ICollection<BlockFragmentBinding> actualBindings)
    {
        if (!visited.Add(blockBox))
        {
            return;
        }

        actualBindings.Add(new BlockFragmentBinding(blockBox, fragment));

        var pendingInlineFlow = new List<DisplayNode>();
        var includeSyntheticListMarker = true;

        foreach (var child in blockBox.Children)
        {
            if (TryQueueInlineFlowChild(child, pendingInlineFlow))
            {
                continue;
            }

            switch (child)
            {
                case BlockBox childBlock when lookup.TryGetValue(childBlock, out var childFragment):
                    FlushPendingInlineFlow(state, blockBox, fragment, pendingInlineFlow, ref includeSyntheticListMarker, observers);

                    if (blockBox.Role == DisplayRole.TableRow)
                    {
                        fragment.AddChild(childFragment);
                        ProcessBlock(state, childBlock, childFragment, lookup, visited, observers, actualBindings);
                        break;
                    }

                    var placedChild = PlaceChildFragmentInFlow(state, blockBox, fragment, childFragment);
                    var deltaY = placedChild.Rect.Y - childBlock.Y;
                    if (Math.Abs(deltaY) > 0.01f)
                    {
                        ShiftBlockSubtree(childBlock, deltaY);
                    }

                    fragment.AddChild(placedChild);
                    ProcessBlock(state, childBlock, placedChild, lookup, visited, observers, actualBindings);
                    break;
            }
        }

        FlushPendingInlineFlow(state, blockBox, fragment, pendingInlineFlow, ref includeSyntheticListMarker, observers);
    }

    private void EmitLineFragments(
        FragmentBuildState state,
        BlockBox blockContext,
        BlockFragment parentFragment,
        IReadOnlyList<IFragmentBuildObserver> observers,
        IReadOnlyList<DisplayNode>? inlineChildren = null,
        bool includeSyntheticListMarker = true)
    {
        var padding = blockContext.Padding.Safe();
        var border = Spacing.FromBorderEdges(blockContext.Style.Borders).Safe();
        var textAlign = blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        var contentLeft = blockContext.X + border.Left + padding.Left;
        var contentTop = blockContext.Y + border.Top + padding.Top;
        var contentWidth = blockContext.Width - padding.Horizontal - border.Horizontal;
        if (contentWidth <= 0f || !float.IsFinite(contentWidth))
        {
            contentWidth = float.PositiveInfinity;
        }

        var runs = CollectInlineRuns(
            blockContext,
            inlineChildren ?? blockContext.Children.ToList(),
            contentWidth,
            state.Context.TextMeasurer,
            includeSyntheticListMarker);
        if (runs.Count == 0)
        {
            return;
        }

        var font = _metrics.GetFontKey(blockContext.Style);
        var fontSize = _metrics.GetFontSize(blockContext.Style);
        var metrics = state.Context.TextMeasurer.GetMetrics(font, fontSize);
        var lineHeight = _lineHeightStrategy.GetLineHeight(blockContext.Style, font, fontSize, metrics);

        var layout = _textLayout.Layout(new TextLayoutInput(runs, contentWidth, lineHeight));
        EmitLineFragmentsFromLayout(
            state,
            blockContext,
            parentFragment,
            layout,
            contentLeft,
            contentTop,
            contentWidth,
            textAlign,
            observers);
    }

    private void EmitLineFragmentsFromLayout(
        FragmentBuildState state,
        BlockBox blockContext,
        BlockFragment parentFragment,
        TextLayoutResult layout,
        float contentLeft,
        float contentTop,
        float contentWidth,
        string textAlign,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        var lastLine = FindLastLine(parentFragment);

        for (var lineIndex = 0; lineIndex < layout.Lines.Count; lineIndex++)
        {
            var line = layout.Lines[lineIndex];
            var topY = lastLine is null ? contentTop : lastLine.Rect.Bottom;
            var baselineY = topY + GetBaselineAscent(line);

            var lineContent = BuildSequentialLineContent(
                state,
                line,
                textAlign,
                contentWidth,
                contentLeft,
                baselineY,
                lineIndex,
                layout.Lines.Count,
                state.Context.TextMeasurer,
                observers);

            lastLine = EmitSequentialLineContent(
                state,
                parentFragment,
                lineContent,
                contentLeft,
                topY,
                line.LineHeight,
                baselineY,
                textAlign,
                lastLine);

            foreach (var source in line.Runs.Select(r => r.Source).Distinct())
            {
                if (lastLine is null)
                {
                    continue;
                }

                foreach (var observer in observers)
                {
                    observer.OnInlineFragmentCreated(source, parentFragment, lastLine);
                }
            }
        }
    }

    // Keep segmentation local to a single line: text-before, inline object, text-after
    // becomes exactly three ordered outputs without any deferred reordering pass.
    private LineContent BuildSequentialLineContent(
        FragmentBuildState state,
        TextLayoutLine line,
        string? textAlign,
        float contentWidth,
        float contentLeft,
        float baselineY,
        int lineIndex,
        int lineCount,
        ITextMeasurer measurer,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        var justifyExtra = ResolveJustifyExtra(textAlign, contentWidth, line.LineWidth, line, lineIndex, lineCount);
        var lineOffsetX = ResolveLineOffset(textAlign, contentWidth, line.LineWidth, line, lineIndex, lineCount);
        var isJustified = justifyExtra > 0f;

        if (isJustified && CountWhitespace(line) > 0)
        {
            return BuildJustifiedSequentialLineContent(
                state,
                line,
                contentLeft,
                baselineY,
                justifyExtra,
                measurer,
                observers);
        }

        var items = new List<LineContentItem>();
        var segmentRuns = new List<TextRun>(line.Runs.Count);
        var currentX = contentLeft + lineOffsetX;

        foreach (var run in line.Runs)
        {
            if (run.InlineObject is not null)
            {
                FlushLineContentSegment(items, segmentRuns);

                var inlineFragment = CreateInlineObjectFragment(
                    state,
                    run.InlineObject,
                    currentX + run.LeftSpacing,
                    baselineY,
                    observers);
                items.Add(LineContentItem.ForInlineObject(inlineFragment));
                currentX += run.LeftSpacing + run.Width + run.RightSpacing;
                continue;
            }

            currentX += run.LeftSpacing;
            var textRun = new TextRun(
                run.Text,
                run.Font,
                run.FontSizePt,
                new PointF(currentX, baselineY),
                run.Width,
                run.Ascent,
                run.Descent,
                run.Decorations,
                run.Color);

            segmentRuns.Add(textRun);
            currentX += run.Width + run.RightSpacing;
        }

        FlushLineContentSegment(items, segmentRuns);
        return new LineContent(items);
    }

    private LineContent BuildJustifiedSequentialLineContent(
        FragmentBuildState state,
        TextLayoutLine line,
        float contentLeft,
        float baselineY,
        float extraSpace,
        ITextMeasurer measurer,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        var spaceCount = CountWhitespace(line);
        if (spaceCount == 0)
        {
            var fallbackItems = new List<LineContentItem>();
            var fallbackRuns = new List<TextRun>(line.Runs.Count);
            var fallbackX = contentLeft;

            foreach (var run in line.Runs)
            {
                if (run.InlineObject is not null)
                {
                    FlushLineContentSegment(fallbackItems, fallbackRuns);

                    var inlineFragment = CreateInlineObjectFragment(
                        state,
                        run.InlineObject,
                        fallbackX + run.LeftSpacing,
                        baselineY,
                        observers);
                    fallbackItems.Add(LineContentItem.ForInlineObject(inlineFragment));
                    fallbackX += run.LeftSpacing + run.Width + run.RightSpacing;
                    continue;
                }

                fallbackX += run.LeftSpacing;
                fallbackRuns.Add(new TextRun(
                    run.Text,
                    run.Font,
                    run.FontSizePt,
                    new PointF(fallbackX, baselineY),
                    run.Width,
                    run.Ascent,
                    run.Descent,
                    run.Decorations,
                    run.Color));
                fallbackX += run.Width + run.RightSpacing;
            }

            FlushLineContentSegment(fallbackItems, fallbackRuns);
            return new LineContent(fallbackItems);
        }

        var perSpaceExtra = extraSpace / spaceCount;
        var items = new List<LineContentItem>();
        var lineRuns = new List<TextRun>();
        var currentX = contentLeft;

        foreach (var run in line.Runs)
        {
            if (run.InlineObject is not null)
            {
                FlushLineContentSegment(items, lineRuns);

                var inlineFragment = CreateInlineObjectFragment(
                    state,
                    run.InlineObject,
                    currentX + run.LeftSpacing,
                    baselineY,
                    observers);
                items.Add(LineContentItem.ForInlineObject(inlineFragment));
                currentX += run.LeftSpacing + run.Width + run.RightSpacing;
                continue;
            }

            var tokens = TextTokenization.Tokenize(run.Text);
            if (tokens.Count == 0)
            {
                continue;
            }

            for (var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
            {
                var token = tokens[tokenIndex];
                var isFirstToken = tokenIndex == 0;
                var isLastToken = tokenIndex == tokens.Count - 1;
                var leftSpacing = isFirstToken ? run.LeftSpacing : 0f;
                var rightSpacing = isLastToken ? run.RightSpacing : 0f;
                var tokenWidth = measurer.MeasureWidth(run.Font, run.FontSizePt, token);
                var whitespaceCount = CountWhitespace(token);
                var tokenExtra = whitespaceCount > 0 ? whitespaceCount * perSpaceExtra : 0f;

                currentX += leftSpacing;
                var textRun = new TextRun(
                    token,
                    run.Font,
                    run.FontSizePt,
                    new PointF(currentX, baselineY),
                    tokenWidth,
                    run.Ascent,
                    run.Descent,
                    run.Decorations,
                    run.Color);

                lineRuns.Add(textRun);
                currentX += tokenWidth + rightSpacing + tokenExtra;
            }
        }

        FlushLineContentSegment(items, lineRuns);
        return new LineContent(items);
    }

    private static float ResolveLineOffset(
        string? textAlign,
        float contentWidth,
        float lineWidth,
        TextLayoutLine line,
        int lineIndex,
        int lineCount)
    {
        if (!float.IsFinite(contentWidth) || contentWidth <= 0f)
        {
            return 0f;
        }

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssConstants.Defaults.TextAlign;
        var extra = Math.Max(0f, contentWidth - lineWidth);

        return align switch
        {
            "center" => extra / 2f,
            "right" => extra,
            "justify" when ShouldJustifyLine(line, lineIndex, lineCount) => 0f,
            _ => 0f
        };
    }

    private static float ResolveJustifyExtra(
        string? textAlign,
        float contentWidth,
        float lineWidth,
        TextLayoutLine line,
        int lineIndex,
        int lineCount)
    {
        if (!float.IsFinite(contentWidth) || contentWidth <= 0f)
        {
            return 0f;
        }

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssConstants.Defaults.TextAlign;
        if (!string.Equals(align, "justify", StringComparison.OrdinalIgnoreCase))
        {
            return 0f;
        }

        if (!ShouldJustifyLine(line, lineIndex, lineCount))
        {
            return 0f;
        }

        return Math.Max(0f, contentWidth - lineWidth);
    }

    private static bool ShouldJustifyLine(TextLayoutLine line, int lineIndex, int lineCount)
    {
        return lineIndex < lineCount - 1;
    }

    private static int CountWhitespace(TextLayoutLine line)
    {
        var count = 0;
        foreach (var run in line.Runs)
        {
            count += CountWhitespace(run.Text);
        }

        return count;
    }

    private static int CountWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var count = 0;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                count++;
            }
        }

        return count;
    }

    private List<TextRunInput> CollectInlineRuns(
        BlockBox blockContext,
        IReadOnlyList<DisplayNode> inlineChildren,
        float availableWidth,
        ITextMeasurer measurer,
        bool includeSyntheticListMarker)
    {
        var runs = new List<TextRunInput>();
        var runId = 1;

        if (includeSyntheticListMarker)
        {
            TryAppendSyntheticListMarkerRun(blockContext, runs, ref runId);
        }

        foreach (var inline in inlineChildren)
        {
            CollectInlineRuns(inline, blockContext.Style, availableWidth, measurer, runs, ref runId);
        }

        return runs;
    }

    private void TryAppendSyntheticListMarkerRun(BlockBox blockContext, ICollection<TextRunInput> runs, ref int runId)
    {
        if (blockContext.Role != DisplayRole.ListItem || blockContext.MarkerOffset > 0f || HasExplicitListMarker(blockContext))
        {
            return;
        }

        var markerText = ResolveListMarkerText(blockContext);
        if (string.IsNullOrWhiteSpace(markerText))
        {
            return;
        }

        var marker = new InlineBox(DisplayRole.Inline)
        {
            TextContent = markerText,
            Style = blockContext.Style,
            Parent = blockContext
        };

        if (_runFactory.TryBuildTextRun(marker, runId, out var markerRun))
        {
            runs.Add(markerRun);
            runId++;
        }
    }

    private static bool HasExplicitListMarker(BlockBox blockContext)
    {
        foreach (var inline in blockContext.Children.OfType<InlineBox>())
        {
            var text = inline.TextContent?.TrimStart();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (text.StartsWith("•", StringComparison.Ordinal) ||
                (char.IsDigit(text[0]) && text.Contains('.')))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveListMarkerText(BlockBox listItem)
    {
        var listContainer = FindNearestListContainer(listItem.Parent);
        if (listContainer is null)
        {
            return string.Empty;
        }

        return ListMarkerResolver.ResolveMarkerText(listContainer, listItem);
    }

    private static DisplayNode? FindNearestListContainer(DisplayNode? node)
    {
        var current = node;
        while (current is not null)
        {
            var tag = current.Element?.TagName;
            if (string.Equals(tag, HtmlCssConstants.HtmlTags.Ul, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, HtmlCssConstants.HtmlTags.Ol, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private BlockFragment CreateInlineObjectFragment(
        FragmentBuildState state,
        InlineObjectLayout inlineObject,
        float left,
        float baselineY,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        var contentBox = inlineObject.ContentBox;
        var padding = contentBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(contentBox.Style.Borders).Safe();
        var margin = contentBox.Style.Margin.Safe();

        var top = baselineY - inlineObject.Baseline;
        contentBox.X = left;
        contentBox.Y = top;
        contentBox.Width = inlineObject.Width;
        contentBox.Height = inlineObject.Height;
        contentBox.Padding = padding;
        contentBox.Margin = margin;
        contentBox.TextAlign = contentBox.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        var fragment = BlockFragmentFactory.Create(contentBox, state);
        foreach (var observer in observers)
        {
            observer.OnBlockFragmentCreated(contentBox, fragment);
        }

        var contentLeft = contentBox.X + border.Left + padding.Left;
        var contentTop = contentBox.Y + border.Top + padding.Top;
        var contentWidth = Math.Max(0f, inlineObject.ContentWidth);

        EmitLineFragmentsFromLayout(
            state,
            contentBox,
            fragment,
            inlineObject.Layout,
            contentLeft,
            contentTop,
            contentWidth,
            contentBox.TextAlign ?? HtmlCssConstants.Defaults.TextAlign,
            observers);

        return fragment;
    }

    private void CollectInlineRuns(
        DisplayNode node,
        ComputedStyle blockStyle,
        float availableWidth,
        ITextMeasurer measurer,
        ICollection<TextRunInput> runs,
        ref int runId)
    {
        switch (node)
        {
            case InlineBlockBoundaryBox boundary:
                if (_runFactory.TryBuildInlineBlockLayout(boundary.SourceInline, availableWidth, measurer, _lineHeightStrategy, out var boundaryLayout) &&
                    _runFactory.TryBuildInlineBlockRun(boundary.SourceInline, runId, boundaryLayout, out var boundaryRun))
                {
                    runs.Add(boundaryRun);
                    runId++;
                    return;
                }

                return;
            case BlockBox block when IsAnonymousInlineWrapper(block):
                foreach (var child in block.Children)
                {
                    CollectInlineRuns(child, blockStyle, availableWidth, measurer, runs, ref runId);
                }

                return;
            case not InlineBox:
                return;
        }

        var inline = (InlineBox)node;

        if (_runFactory.TryBuildInlineBlockLayout(inline, availableWidth, measurer, _lineHeightStrategy, out var inlineLayout))
        {
            if (_runFactory.TryBuildInlineBlockRun(inline, runId, inlineLayout, out var inlineRun))
            {
                runs.Add(inlineRun);
                runId++;
                return;
            }
        }

        if (_runFactory.TryBuildLineBreakRunFromBlockContext(inline, blockStyle, runId, out var lineBreakRun))
        {
            runs.Add(lineBreakRun);
            runId++;
            return;
        }

        if (_runFactory.TryBuildTextRun(inline, runId, out var textRun))
        {
            runs.Add(textRun);
            runId++;
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(childInline, blockStyle, availableWidth, measurer, runs, ref runId);
        }
    }

    private static LineBoxFragment? FindLastLine(BlockFragment parentFragment)
    {
        if (parentFragment.Children.Count == 0)
        {
            return null;
        }

        for (var i = parentFragment.Children.Count - 1; i >= 0; i--)
        {
            if (parentFragment.Children[i] is LineBoxFragment line)
            {
                return line;
            }
        }

        return null;
    }

    private static float GetBaselineAscent(TextLayoutLine line)
    {
        if (line.Runs.Count == 0)
        {
            return 0f;
        }

        var ascent = 0f;
        foreach (var run in line.Runs)
        {
            ascent = Math.Max(ascent, run.Ascent);
        }

        return ascent;
    }

    private static LineBoxFragment CreateLineFragment(
        FragmentBuildState state,
        IReadOnlyList<TextRun> lineRuns,
        float fallbackX,
        float topY,
        float lineHeight,
        float baselineY,
        string? textAlign)
    {
        var minX = lineRuns.Min(static run => run.Origin.X);
        var maxX = lineRuns.Max(static run => run.Origin.X + run.AdvanceWidth);
        var width = Math.Max(0f, maxX - minX);

        return new LineBoxFragment
        {
            FragmentId = state.ReserveFragmentId(),
            PageNumber = state.PageNumber,
            Rect = new RectangleF(Math.Min(fallbackX, minX), topY, width, lineHeight),
            BaselineY = baselineY,
            LineHeight = lineHeight,
            Runs = lineRuns.ToList(),
            TextAlign = textAlign?.ToLowerInvariant()
        };
    }

    private static LineBoxFragment? EmitSequentialLineContent(
        FragmentBuildState state,
        BlockFragment parentFragment,
        LineContent lineContent,
        float contentLeft,
        float topY,
        float lineHeight,
        float baselineY,
        string? textAlign,
        LineBoxFragment? lastLine)
    {
        foreach (var item in lineContent.Items)
        {
            if (item.LineRuns.Count > 0)
            {
                lastLine = CreateLineFragment(
                    state,
                    item.LineRuns,
                    contentLeft,
                    topY,
                    lineHeight,
                    baselineY,
                    textAlign);
                parentFragment.AddChild(lastLine);
                continue;
            }

            if (item.InlineObject is not null)
            {
                parentFragment.AddChild(item.InlineObject);
            }
        }

        return lastLine;
    }

    private void FlushPendingInlineFlow(
        FragmentBuildState state,
        BlockBox blockContext,
        BlockFragment parentFragment,
        List<DisplayNode> pendingInlineFlow,
        ref bool includeSyntheticListMarker,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return;
        }

        EmitLineFragments(
            state,
            blockContext,
            parentFragment,
            observers,
            pendingInlineFlow,
            includeSyntheticListMarker);

        includeSyntheticListMarker = false;
        pendingInlineFlow.Clear();
    }

    private static bool TryQueueInlineFlowChild(DisplayNode child, ICollection<DisplayNode> pendingInlineFlow)
    {
        switch (child)
        {
            case InlineBox:
            case InlineBlockBoundaryBox:
                pendingInlineFlow.Add(child);
                return true;
            case BlockBox block when IsAnonymousInlineWrapper(block):
                pendingInlineFlow.Add(block);
                return true;
            default:
                return false;
        }
    }

    private static bool IsAnonymousInlineWrapper(BlockBox block)
    {
        return block.IsAnonymous &&
               block.Children.Count > 0 &&
               block.Children.All(static child => child is InlineBox);
    }

    private BlockFragment PlaceChildFragmentInFlow(
        FragmentBuildState state,
        BlockBox parentBlock,
        BlockFragment parentFragment,
        BlockFragment childFragment)
    {
        var placementY = Math.Max(childFragment.Rect.Y, ResolveNextChildTop(parentBlock, parentFragment));
        if (Math.Abs(placementY - childFragment.Rect.Y) < 0.01f)
        {
            return childFragment;
        }

        return (BlockFragment)FragmentCoordinateTranslator.CloneWithPlacement(
            childFragment,
            state.PageNumber,
            childFragment.Rect.X,
            placementY);
    }

    private static float ResolveNextChildTop(BlockBox parentBlock, BlockFragment parentFragment)
    {
        var padding = parentBlock.Padding.Safe();
        var border = Spacing.FromBorderEdges(parentBlock.Style.Borders).Safe();
        var contentTop = parentBlock.Y + border.Top + padding.Top;

        if (parentFragment.Children.Count == 0)
        {
            return contentTop;
        }

        return parentFragment.Children[^1].Rect.Bottom;
    }

    private static void ShiftBlockSubtree(BlockBox root, float deltaY)
    {
        root.Y += deltaY;

        foreach (var child in root.Children.OfType<BlockBox>())
        {
            ShiftBlockSubtree(child, deltaY);
        }
    }

    private static void FlushLineContentSegment(ICollection<LineContentItem> items, List<TextRun> segmentRuns)
    {
        if (segmentRuns.Count == 0)
        {
            return;
        }

        items.Add(LineContentItem.ForTextRuns(segmentRuns.ToList()));
        segmentRuns.Clear();
    }

    private sealed record LineContent(IReadOnlyList<LineContentItem> Items);

    private sealed record LineContentItem(IReadOnlyList<TextRun> LineRuns, BlockFragment? InlineObject)
    {
        public static LineContentItem ForTextRuns(IReadOnlyList<TextRun> lineRuns) => new(lineRuns, null);

        public static LineContentItem ForInlineObject(BlockFragment inlineObject) => new([], inlineObject);
    }

    private sealed class FallbackTextMeasurer : ITextMeasurer
    {
        public float MeasureWidth(FontKey font, float sizePt, string text) => text.Length;
        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) => (0f, 0f);
    }

}
