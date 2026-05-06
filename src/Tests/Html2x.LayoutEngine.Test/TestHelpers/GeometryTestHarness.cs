using System.Globalization;
using System.Text;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Html2x.Text;
using Html2x.LayoutEngine.Test.TestDoubles;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class GeometryTestHarness
{
    public static async Task<GeometryPipelineResult> BuildAsync(
        string html,
        LayoutBuildSettings? options = null)
    {
        options ??= new()
        {
            PageSize = PaperSizes.Letter
        };

        var diagnosticsSink = new HarnessDiagnosticsSink();
        var textMeasurer = CreateTextMeasurer();
        var imageMetadataResolver = new NoopImageMetadataResolver();
        var styleTreeBuilder = new StyleTreeBuilder();
        var layoutGeometryBuilder = new LayoutGeometryBuilder(textMeasurer);
        var fragmentBuilder = new FragmentBuilder();

        var styleTree = await styleTreeBuilder.BuildAsync(html, options.Style, diagnosticsSink: diagnosticsSink);
        var geometryRequest = new LayoutGeometryRequest
        {
            PageSize = options.PageSize,
            ImageMetadataResolver = imageMetadataResolver,
            ResourceBaseDirectory = Directory.GetCurrentDirectory(),
            MaxImageSizeBytes = 10 * 1024 * 1024
        };
        var publishedLayout = layoutGeometryBuilder.Build(
            styleTree,
            geometryRequest,
            diagnosticsSink);
        var fragments = fragmentBuilder.Build(publishedLayout);
        var pagination = new LayoutPaginator().Paginate(
            fragments.Blocks,
            new()
            {
                PageSize = options.PageSize,
                Margin = publishedLayout.Page.Margin
            },
            diagnosticsSink);
        var snapshot = GeometrySnapshotMapper.From(publishedLayout, pagination);

        return new(
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

    public static string NormalizeNewLines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);

    public static ITextMeasurer CreateTextMeasurer() => new ConstantTextMeasurer(10f, 9f, 3f);

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
            extras.Add(
                $"occupied={FormatRect(fragment.OccupiedX.Value, fragment.OccupiedY.Value, fragment.OccupiedSize.Value)}");
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

    private static string FormatRect(float x, float y, SizePt size) =>
        $"{FormatFloat(x)},{FormatFloat(y)},{FormatFloat(size.Width)},{FormatFloat(size.Height)}";

    private static string FormatSize(SizePt size) => $"{FormatFloat(size.Width)},{FormatFloat(size.Height)}";

    private static string FormatSpacing(Spacing spacing) =>
        $"{FormatFloat(spacing.Top)},{FormatFloat(spacing.Right)},{FormatFloat(spacing.Bottom)},{FormatFloat(spacing.Left)}";

    private static string FormatFloat(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

internal sealed record GeometryPipelineResult(
    PublishedLayoutTree PublishedLayout,
    FragmentTree Fragments,
    HtmlLayout Layout,
    PaginationResult Pagination,
    GeometrySnapshot Snapshot,
    IReadOnlyList<DiagnosticRecord> Diagnostics);

internal sealed class HarnessDiagnosticsSink : IDiagnosticsSink
{
    private readonly List<DiagnosticRecord> _records = [];

    public IReadOnlyList<DiagnosticRecord> Records => _records;

    public void Emit(DiagnosticRecord record)
    {
        _records.Add(record);
    }
}