using System.Text;
using AngleSharp;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class GeometryTestHarness
{
    public static async Task<GeometryPipelineResult> BuildAsync(
        string html,
        LayoutOptions? options = null)
    {
        options ??= new LayoutOptions
        {
            PageSize = PaperSizes.Letter
        };

        var diagnosticsSession = new DiagnosticsSession();
        var textMeasurer = CreateTextMeasurer();
        var fontSource = LayoutBuilderFixture.CreateFontSource();
        var imageProvider = new NoopImageProvider();
        var blockFormattingContext = new BlockFormattingContext();
        var domProvider = new AngleSharpDomProvider(Configuration.Default.WithCss());
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new CssValueConverter());
        var boxBuilder = new BoxTreeBuilder(textMeasurer, blockFormattingContext);
        var observer = new RecordingFragmentBuildObserver();
        var fragmentBuilder = new FragmentBuilder([observer]);

        var document = await domProvider.LoadAsync(html, options);
        var styleTree = styleComputer.Compute(document, diagnosticsSession);
        var boxTree = boxBuilder.Build(styleTree, diagnosticsSession);
        LayoutSnapshotMapper.ValidateInlineBlockStructures(boxTree, diagnosticsSession);

        var fragmentContext = new FragmentBuildContext(
            imageProvider,
            Directory.GetCurrentDirectory(),
            10 * 1024 * 1024,
            textMeasurer,
            fontSource,
            blockFormattingContext);
        var fragments = fragmentBuilder.Build(boxTree, fragmentContext);
        var pagination = new BlockPaginator().Paginate(
            fragments.Blocks,
            options.PageSize,
            boxTree.Page.Margin,
            diagnosticsSession);
        var layout = CreateLayout(pagination);
        var snapshot = GeometrySnapshotMapper.From(boxTree, layout, pagination);

        return new GeometryPipelineResult(boxTree, fragments, layout, pagination, snapshot, observer);
    }

    public static string RenderSnapshot(GeometrySnapshot snapshot)
    {
        var builder = new StringBuilder();

        builder.AppendLine("boxes");
        foreach (var box in snapshot.Boxes)
        {
            AppendBox(builder, box, 0);
        }

        builder.AppendLine("fragments");
        foreach (var page in snapshot.Fragments.Pages)
        {
            builder.AppendLine(
                $"page {page.PageNumber} size={FormatSize(page.PageSize)} margin={FormatSpacing(page.Margin)}");

            foreach (var fragment in page.Fragments)
            {
                AppendFragment(builder, fragment, 1);
            }
        }

        builder.AppendLine("pagination");
        foreach (var page in snapshot.Pagination)
        {
            builder.AppendLine(
                $"page {page.PageNumber} content={FormatFloat(page.ContentTop)}..{FormatFloat(page.ContentBottom)}");

            foreach (var placement in page.Placements)
            {
                builder.AppendLine(
                    $"  placement order={placement.OrderIndex} fragment={placement.FragmentId} kind={placement.Kind} oversized={placement.IsOversized.ToString().ToLowerInvariant()} rect={FormatRect(placement.X, placement.Y, placement.Size)}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    public static ITextMeasurer CreateTextMeasurer()
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(10f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((9f, 3f));
        return textMeasurer.Object;
    }

    private static HtmlLayout CreateLayout(PaginationResult pagination)
    {
        var layout = new HtmlLayout();

        foreach (var page in pagination.Pages)
        {
            layout.Pages.Add(new LayoutPage(
                page.PageSize,
                page.Margins,
                page.Placements.Select(static placement => (LayoutFragment)placement.Fragment).ToList(),
                page.PageNumber));
        }

        return layout;
    }

    private static void AppendBox(StringBuilder builder, BoxGeometrySnapshot box, int depth)
    {
        var indent = new string(' ', depth * 2);
        var content = box.ContentSize is null || box.ContentX is null || box.ContentY is null
            ? "none"
            : FormatRect(box.ContentX.Value, box.ContentY.Value, box.ContentSize.Value);
        var extras = new List<string>
        {
            $"marker={FormatFloat(box.MarkerOffset)}",
            $"anonymous={box.IsAnonymous.ToString().ToLowerInvariant()}",
            $"inlineBlock={box.IsInlineBlockContext.ToString().ToLowerInvariant()}"
        };

        if (box.DerivedColumnCount.HasValue)
        {
            extras.Add($"columns={box.DerivedColumnCount.Value}");
        }

        if (box.RowIndex.HasValue)
        {
            extras.Add($"row={box.RowIndex.Value}");
        }

        if (box.ColumnIndex.HasValue)
        {
            extras.Add($"column={box.ColumnIndex.Value}");
        }

        if (box.IsHeader.HasValue)
        {
            extras.Add($"header={box.IsHeader.Value.ToString().ToLowerInvariant()}");
        }

        builder.AppendLine(
            $"{indent}{box.SequenceId}:{box.Kind} path={box.Path} rect={FormatRect(box.X, box.Y, box.Size)} content={content} {string.Join(" ", extras)}");

        foreach (var child in box.Children)
        {
            AppendBox(builder, child, depth + 1);
        }
    }

    private static void AppendFragment(StringBuilder builder, FragmentSnapshot fragment, int depth)
    {
        var indent = new string(' ', depth * 2);
        var extras = new List<string>();

        if (!string.IsNullOrWhiteSpace(fragment.Text))
        {
            extras.Add($"text=\"{fragment.Text}\"");
        }

        if (fragment.MarkerOffset.HasValue)
        {
            extras.Add($"marker={FormatFloat(fragment.MarkerOffset.Value)}");
        }

        if (fragment.DerivedColumnCount.HasValue)
        {
            extras.Add($"columns={fragment.DerivedColumnCount.Value}");
        }

        if (fragment.RowIndex.HasValue)
        {
            extras.Add($"row={fragment.RowIndex.Value}");
        }

        if (fragment.ColumnIndex.HasValue)
        {
            extras.Add($"column={fragment.ColumnIndex.Value}");
        }

        if (fragment.IsHeader.HasValue)
        {
            extras.Add($"header={fragment.IsHeader.Value.ToString().ToLowerInvariant()}");
        }

        builder.AppendLine(
            $"{indent}{fragment.SequenceId}:{fragment.Kind} rect={FormatRect(fragment.X, fragment.Y, fragment.Size)}{(extras.Count == 0 ? string.Empty : $" {string.Join(" ", extras)}")}");

        foreach (var child in fragment.Children)
        {
            AppendFragment(builder, child, depth + 1);
        }
    }

    private static string FormatRect(float x, float y, SizePt size)
    {
        return $"{FormatFloat(x)},{FormatFloat(y)},{FormatFloat(size.Width)},{FormatFloat(size.Height)}";
    }

    private static string FormatSize(SizePt size)
    {
        return $"{FormatFloat(size.Width)},{FormatFloat(size.Height)}";
    }

    private static string FormatSpacing(Spacing spacing)
    {
        return $"{FormatFloat(spacing.Top)},{FormatFloat(spacing.Right)},{FormatFloat(spacing.Bottom)},{FormatFloat(spacing.Left)}";
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}

internal sealed record GeometryPipelineResult(
    BoxTree BoxTree,
    FragmentTree Fragments,
    HtmlLayout Layout,
    PaginationResult Pagination,
    GeometrySnapshot Snapshot,
    RecordingFragmentBuildObserver Observer);

internal sealed record BlockBinding(BlockBox Source, BlockFragment Fragment);

internal sealed record SpecialBinding(DisplayNode Source, LayoutFragment Fragment);

internal sealed class RecordingFragmentBuildObserver : IFragmentBuildObserver
{
    public List<BlockBinding> BlockBindings { get; } = [];

    public List<SpecialBinding> SpecialBindings { get; } = [];

    public void OnBlockFragmentCreated(BlockBox source, BlockFragment fragment)
    {
        BlockBindings.Add(new BlockBinding(source, fragment));
    }

    public void OnInlineFragmentCreated(InlineBox source, BlockFragment parent, LineBoxFragment line)
    {
    }

    public void OnSpecialFragmentCreated(DisplayNode source, LayoutFragment fragment)
    {
        SpecialBindings.Add(new SpecialBinding(source, fragment));
    }

    public void OnZOrderCompleted(IReadOnlyList<LayoutFragment> fragments)
    {
    }
}

internal sealed class ReferenceEqualityComparer : IEqualityComparer<BlockBox>
{
    public static ReferenceEqualityComparer Instance { get; } = new();

    public bool Equals(BlockBox? x, BlockBox? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(BlockBox obj)
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
