using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Construction;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Geometry.Publishing;
using Html2x.LayoutEngine.Geometry.Style;
using Html2x.LayoutEngine.Geometry.Tables;
using Html2x.LayoutEngine.Geometry.Writing;
using Html2x.LayoutEngine.Pagination;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.Resources;
using Shouldly;
using Html2x.Architecture.Test.Support;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test.Layout;

public sealed class LayoutGeometryOwnerTests
{
    [Fact]
    public void GeometryOwnerMap_DocumentsAcceptedOwners()
    {
        DocumentAnchors.InternalGeometry.GeometryOwners
            .ShouldMentionTopics(
                DocumentTopic.NamespaceSegmentOf<BoxTreeConstruction>(),
                DocumentTopic.NamespaceSegmentOf<BlockFlowLayout>(),
                DocumentTopic.NamespaceSegmentOf<InlineFlowLayout>(),
                DocumentTopic.NamespaceSegmentOf<BlockFormattingMetricsMeasurement>(),
                DocumentTopic.NamespaceSegmentOf<TableBlockLayout>(),
                DocumentTopic.NamespaceSegmentOf<ImageSizingRules>(),
                DocumentTopic.NamespaceSegmentOf<PublishedLayoutWriter>(),
                DocumentTopic.NamespaceSegmentOf(typeof(TableGridDiagnostics)),
                DocumentTopic.NamespaceSegmentOf(typeof(BoxDimensionRules)),
                DocumentTopic.NamespaceSegmentOf(typeof(HtmlElementRules)),
                DocumentTopic.NamespaceSegmentOf<LayoutBoxStateWriter>(),
                DocumentTopic.NamespaceSegmentOf<BlockBox>());
    }

    [Fact]
    public void GeometrySourceDirectories_UseAcceptedOwnerNames()
    {
        Directory.GetDirectories(SourceDirectoryFor<LayoutGeometryConstruction>())
            .Select(Path.GetFileName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Where(static name => name is not RepositoryLayout.BinDirectory
                and not RepositoryLayout.ObjDirectory
                and not "Properties")
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ShouldBe([
                "BlockFlow",
                "Construction",
                "Diagnostics",
                "Images",
                "InlineFlow",
                "Measurement",
                "Models",
                "Primitives",
                "Publishing",
                "Style",
                "Tables",
                "Writing"
            ]);
    }

    [Fact]
    public void ConstructionOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        var construction = SourceSetForNamespaceOf<BoxTreeConstruction>();

        construction.ShouldDeclareNamespace(NamespaceOf<BoxTreeConstruction>());
        SourceFileFor<BoxTreeConstruction>()
            .ShouldContainType<BoxTreeConstruction>(InternalAccessibility, true);
        SourceFileFor(typeof(BoxTreeNormalization))
            .ShouldContainType(typeof(BoxTreeNormalization), InternalAccessibility);
        SourceFileFor(typeof(StringNormalizer))
            .ShouldContainType(typeof(StringNormalizer), InternalAccessibility);
        SourceFileFor<TextNormalizationState>()
            .ShouldContainType<TextNormalizationState>(InternalAccessibility, true);
        SourceFileFor(typeof(ListMarkerPolicy))
            .ShouldContainType(typeof(ListMarkerPolicy), InternalAccessibility);

        construction.ShouldNotUseNamespaces(
            NamespaceOf<PublishedLayoutWriter>(),
            NamespaceOf<FragmentTreeBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            RendererNamespace,
            AssemblyName(typeof(ImageResourceLoader)),
            ParserPackageName());
        construction.ShouldNotUseIdentifiers(
            nameof(LayoutGeometryConstruction),
            nameof(FragmentTreeBuilder),
            nameof(LayoutPaginator),
            nameof(PublishedLayoutWriter),
            nameof(ImageResourceLoader));
    }

    [Fact]
    public void ImagesOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        var images = SourceSetForNamespaceOf<ImageSizingRules>();

        images.ShouldDeclareNamespace(NamespaceOf<ImageSizingRules>());
        SourceFileFor<ImageSizingRules>()
            .ShouldContainType<ImageSizingRules>(InternalAccessibility, true);
        SourceFileFor<ImageBlockLayoutRule>()
            .ShouldContainType<ImageBlockLayoutRule>(InternalAccessibility, true);
        SourceFileFor<ImageBlockLayoutWriter>()
            .ShouldContainType<ImageBlockLayoutWriter>(InternalAccessibility, true);
        SourceFileFor<ImageLayoutResolution>()
            .ShouldContainRecordStruct<ImageLayoutResolution>(InternalAccessibility);

        images.ShouldNotUseNamespaces(
            NamespaceOf<PublishedLayoutWriter>(),
            NamespaceOf<FragmentTreeBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            RendererNamespace,
            AssemblyName(typeof(ImageResourceLoader)),
            ParserPackageName());
        images.ShouldNotUseIdentifiers(
            nameof(LayoutGeometryConstruction),
            nameof(FragmentTreeBuilder),
            nameof(LayoutPaginator),
            nameof(PublishedLayoutWriter),
            nameof(ImageResourceLoader));
    }

    [Fact]
    public void BlockFlowOwner_DoesNotPublishLayoutContracts()
    {
        var blockFlowSources = SourceSetForNamespaceOf<BlockFlowLayout>();
        var blockFlow = SourceFileFor<BlockFlowLayout>();

        blockFlowSources.ShouldDeclareNamespace(NamespaceOf<BlockFlowLayout>());

        blockFlow.ShouldNotUseNamespace(NamespaceOf<PublishedLayoutWriter>());
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedBlock));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedInlineLayout));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedBlockFlowItem));
        blockFlow.ShouldUseIdentifier(nameof(BlockFlowItemLayout));
    }

    [Fact]
    public void PublishingOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        var publishing = SourceSetForNamespaceOf<PublishedLayoutWriter>();

        publishing.ShouldDeclareNamespace(NamespaceOf<PublishedLayoutWriter>());
        SourceFileFor<PublishedLayoutWriter>()
            .ShouldContainType<PublishedLayoutWriter>(InternalAccessibility, true);
        SourceFileFor(typeof(PublishedBlockFacts))
            .ShouldContainType(typeof(PublishedBlockFacts), InternalAccessibility);

        publishing.ShouldNotUseNamespaces(
            NamespaceOf<FragmentTreeBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            RendererNamespace,
            AssemblyName(typeof(ImageResourceLoader)),
            ParserPackageName());
        publishing.ShouldNotUseIdentifiers(
            nameof(LayoutGeometryConstruction),
            nameof(FragmentTreeBuilder),
            nameof(LayoutPaginator),
            nameof(ImageResourceLoader));
    }

    [Fact]
    public void MeasurementOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        var measurement = SourceSetForNamespaceOf<BlockFormattingMetricsMeasurement>();

        measurement.ShouldDeclareNamespace(NamespaceOf<BlockFormattingMetricsMeasurement>());
        SourceFileFor<BlockFormattingMetricsMeasurement>()
            .ShouldContainType<BlockFormattingMetricsMeasurement>(InternalAccessibility, true);

        measurement.ShouldNotUseNamespaces(
            NamespaceOf<PublishedLayoutWriter>(),
            NamespaceOf<FragmentTreeBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            RendererNamespace,
            AssemblyName(typeof(ImageResourceLoader)),
            ParserPackageName());
        measurement.ShouldNotUseIdentifiers(
            nameof(LayoutGeometryConstruction),
            nameof(FragmentTreeBuilder),
            nameof(LayoutPaginator),
            nameof(PublishedLayoutWriter),
            nameof(ImageResourceLoader));
    }

    [Fact]
    public void InlineFlowOwner_UsesSharedTextMeasurementAndBaselineRules()
    {
        SourceSetForNamespaceOf<InlineFlowLayout>()
            .ShouldDeclareNamespace(NamespaceOf<InlineFlowLayout>());
        SourceFileFor<InlineTextLayoutMeasurement>()
            .ShouldUseIdentifier(nameof(InlineRunCollector));
        SourceFileFor<InlineTextLayoutMeasurement>()
            .ShouldUseIdentifier(nameof(TextLineLayout));
        SourceFileFor<InlineFlowMeasurement>()
            .ShouldUseIdentifier(nameof(InlineTextLayoutMeasurement));
        SourceFileFor<AtomicInlineBoxLayout>()
            .ShouldUseIdentifier(nameof(InlineTextLayoutMeasurement));
        SourceFileFor<InlineFlowLayout>()
            .ShouldUseIdentifier(nameof(InlineLayoutWriter));
        SourceFileFor<InlineLayoutWriter>()
            .ShouldUseIdentifier(nameof(InlineBaselineRules));
        SourceFileFor<InlineBoxContentStateWriter>()
            .ShouldContainType<InlineBoxContentStateWriter>(InternalAccessibility, true);
        SourceFileFor<AtomicInlineBoxLayout>()
            .ShouldUseIdentifier(nameof(InlineBaselineRules));
    }

    [Fact]
    public void PrimitiveWritingAndStyleOwners_HaveExpectedNamespaces()
    {
        SourceSetForNamespaceOf(typeof(BoxDimensionRules))
            .ShouldDeclareNamespace(NamespaceOf(typeof(BoxDimensionRules)));
        SourceSetForNamespaceOf<LayoutBoxStateWriter>()
            .ShouldDeclareNamespace(NamespaceOf<LayoutBoxStateWriter>());
        SourceSetForNamespaceOf(typeof(HtmlElementRules))
            .ShouldDeclareNamespace(NamespaceOf(typeof(HtmlElementRules)));
    }

    [Fact]
    public void TablesOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        DocumentAnchors.InternalGeometry.GeometryOwners
            .ShouldMentionTopics(
                DocumentTopic.NamespaceOf<TableBlockLayout>(),
                DocumentTopic.NamespaceOf<ImageSizingRules>(),
                DocumentTopic.Type(typeof(TableGridDiagnostics)),
                DocumentTopic.NamespaceSegmentOf(typeof(TableGridDiagnostics)),
                DocumentTopic.NamespaceSegmentOf<TableBox>());

        var tableSourceSet = SourceSetForNamespaceOf<TableBlockLayout>();
        tableSourceSet.ShouldDeclareNamespace(TablesNamespace);

        var tables = TableOwnerSources()
            .Select(static source => new
            {
                Source = SourceFileFor(source.Type),
                TypeName = source.Type.Name,
                source.IsRecordStruct
            })
            .ToArray();

        foreach (var table in tables)
        {
            if (table.IsRecordStruct)
            {
                table.Source.ShouldContainRecordStruct(table.TypeName, InternalAccessibility);
            }
            else
            {
                table.Source.ShouldContainType(table.TypeName, InternalAccessibility);
            }

            table.Source.ShouldNotUseNamespaces(
                NamespaceOf<PublishedLayoutWriter>(),
                NamespaceOf<FragmentTreeBuilder>(),
                NamespaceOf<LayoutPaginator>(),
                RendererNamespace,
                AssemblyName(typeof(ImageResourceLoader)),
                ParserPackageName());
            table.Source.ShouldNotUseIdentifiers(
                nameof(LayoutGeometryConstruction),
                nameof(FragmentTreeBuilder),
                nameof(LayoutPaginator),
                nameof(PublishedLayoutWriter),
                nameof(ImageResourceLoader));
        }

        var diagnostics = SourceFileFor(typeof(TableGridDiagnostics));
        diagnostics.ShouldDeclareNamespace(DiagnosticsNamespace);
        diagnostics.ShouldContainType(typeof(TableGridDiagnostics), InternalAccessibility);
    }

    private static readonly string TablesNamespace = NamespaceOf<TableBlockLayout>();

    private static readonly string DiagnosticsNamespace = NamespaceOf(typeof(TableGridDiagnostics));

    private static readonly string RendererNamespace = NamespacePrefix(
        NamespaceOf<PdfRenderer>(),
        2);

    private static IReadOnlyList<TableOwnerSource> TableOwnerSources() =>
    [
        new(typeof(TableBlockLayout)),
        new(typeof(TableBlockLayoutRule)),
        new(typeof(TableGridLayout)),
        new(typeof(TableLayoutCellPlacement)),
        new(typeof(TableLayoutResult)),
        new(typeof(TableLayoutRowResult)),
        new(typeof(TableBoxStateWriter)),
        new(typeof(TablePlacementWriter)),
        new(typeof(TableStructure)),
        new(typeof(TableStructureResult), true),
        new(typeof(TableStructureDiagnosticNames)),
        new(typeof(TableGridDiagnosticNames)),
        new(typeof(TableRowDiagnosticFacts)),
        new(typeof(TableCellDiagnosticFacts)),
        new(typeof(TableColumnDiagnosticFacts)),
        new(typeof(TableGroupDiagnosticFacts))
    ];

    private sealed record TableOwnerSource(
        Type Type,
        bool IsRecordStruct = false);
}
