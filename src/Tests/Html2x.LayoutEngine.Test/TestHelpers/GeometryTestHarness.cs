using System.Text;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Style;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using LayoutFragment = Html2x.RenderModel.Fragment;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class GeometryTestHarness
{
    public static async Task<GeometryPipelineResult> BuildAsync(
        string html,
        LayoutBuildSettings? options = null)
    {
        options ??= new LayoutBuildSettings
        {
            PageSize = PaperSizes.Letter
        };

        var diagnosticsSink = new HarnessDiagnosticsSink();
        var textMeasurer = CreateTextMeasurer();
        var imageMetadataResolver = new NoopImageMetadataResolver();
        var styleTreeBuilder = new StyleTreeBuilder();
        var boxBuilder = new BoxTreeBuilder(textMeasurer);
        var layoutGeometryBuilder = new LayoutGeometryBuilder(textMeasurer);
        var fragmentBuilder = new FragmentBuilder();

        var styleTree = await styleTreeBuilder.BuildAsync(html, options.Style, diagnosticsSink: diagnosticsSink);
        var geometryRequest = new LayoutGeometryRequest
        {
            PageSize = options.PageSize,
            ImageMetadataResolver = imageMetadataResolver,
            HtmlDirectory = Directory.GetCurrentDirectory(),
            MaxImageSizeBytes = 10 * 1024 * 1024
        };
        var boxTree = boxBuilder.Build(styleTree, geometryRequest, diagnosticsSink);
        var publishedLayout = layoutGeometryBuilder.Build(
            styleTree,
            geometryRequest,
            diagnosticsSink);
        var fragments = fragmentBuilder.Build(publishedLayout);
        var pagination = new LayoutPaginator().Paginate(
            fragments.Blocks,
            new PaginationOptions
            {
                PageSize = options.PageSize,
                Margin = publishedLayout.Page.Margin
            },
            diagnosticsSink);
        var snapshot = GeometrySnapshotMapper.From(publishedLayout, pagination);

        return new GeometryPipelineResult(
            boxTree,
            publishedLayout,
            fragments,
            pagination.Layout,
            pagination,
            snapshot,
            diagnosticsSink.Records);
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
                    $"  placement order={placement.OrderIndex} fragment={placement.FragmentId} kind={placement.Kind} decision={placement.DecisionKind} oversized={placement.IsOversized.ToString().ToLowerInvariant()} rect={FormatRect(placement.X, placement.Y, placement.Size)}");
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
        textMeasurer.Setup(x => x.Measure(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey font, float _, string _) => TextMeasurement.CreateFallback(font, 10f, 9f, 3f));
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(10f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((9f, 3f));
        return textMeasurer.Object;
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

        if (fragment.OccupiedX.HasValue && fragment.OccupiedY.HasValue && fragment.OccupiedSize.HasValue)
        {
            extras.Add($"occupied={FormatRect(fragment.OccupiedX.Value, fragment.OccupiedY.Value, fragment.OccupiedSize.Value)}");
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
    PublishedLayoutTree PublishedLayout,
    FragmentTree Fragments,
    HtmlLayout Layout,
    PaginationResult Pagination,
    GeometrySnapshot Snapshot,
    IReadOnlyList<DiagnosticRecord> Diagnostics);

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

internal sealed class HarnessDiagnosticsSink : IDiagnosticsSink
{
    private readonly List<DiagnosticRecord> _records = [];

    public IReadOnlyList<DiagnosticRecord> Records => _records;

    public void Emit(DiagnosticRecord record)
    {
        _records.Add(record);
    }
}
