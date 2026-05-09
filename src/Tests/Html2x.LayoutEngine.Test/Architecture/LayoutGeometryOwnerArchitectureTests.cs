using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Composition;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Publishing;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Pagination;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryOwnerArchitectureTests
{
    [Fact]
    public void GeometryOwnerMap_DocumentsAcceptedOwners()
    {
        ArchitectureDocument.Load("docs", "internals", "geometry.md")
            .ShouldMentionTopicsInSection(
                "Geometry Owners",
                "Construction",
                "BlockFlow",
                "InlineFlow",
                "Measurement",
                "Tables",
                "Images",
                "Publishing",
                "Diagnostics",
                "Primitives",
                "Models");
    }

    [Fact]
    public void ImagesOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        var images = CSharpSourceSet.FromDirectory("src", AssemblyName<LayoutGeometryBuilder>(), "Images");

        images.ShouldDeclareNamespace(NamespaceOf<ImageSizingRules>());
        SourceFileFor<ImageSizingRules>("Images")
            .ShouldContainType(nameof(ImageSizingRules), InternalAccessibility, true);
        SourceFileFor<ImageBlockLayoutRule>("Images")
            .ShouldContainType(nameof(ImageBlockLayoutRule), InternalAccessibility, true);
        SourceFileFor<ImageBlockLayoutWriter>("Images")
            .ShouldContainType(nameof(ImageBlockLayoutWriter), InternalAccessibility, true);
        SourceFileFor<ImageLayoutResolution>("Images")
            .ShouldContainRecordStruct(nameof(ImageLayoutResolution), InternalAccessibility);

        images.ShouldNotUseNamespaces(
            NamespaceOf<PublishedLayoutWriter>(),
            "Html2x.LayoutEngine.Geometry.Publishing",
            NamespaceOf<FragmentBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            "Html2x.Renderers",
            ResourcesAssemblyName,
            ParserPackageName(),
            "Html2x.LayoutEngine.Geometry.Composition");
        images.ShouldNotUseIdentifiers(
            nameof(LayoutGeometryBuilder),
            nameof(GeometryPipelineComposer),
            nameof(FragmentBuilder),
            nameof(LayoutPaginator),
            nameof(PublishedLayoutWriter),
            "IImageSizingRules",
            "ImageResourceLoader");
    }

    [Fact]
    public void BlockFlowOwner_DoesNotPublishLayoutContracts()
    {
        var blockFlow = SourceFileFor<BlockFlowLayout>("Box");

        blockFlow.ShouldNotUseNamespace(NamespaceOf<PublishedLayoutWriter>());
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
        blockFlow.ShouldNotUseIdentifier("PublishedBlock");
        blockFlow.ShouldNotUseIdentifier("PublishedInlineLayout");
        blockFlow.ShouldNotUseIdentifier("PublishedBlockFlowItem");
        blockFlow.ShouldUseIdentifier(nameof(BlockFlowItemLayout));
    }

    [Fact]
    public void PublishingOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        var publishing = CSharpSourceSet.FromDirectory("src", AssemblyName<LayoutGeometryBuilder>(), "Publishing");

        publishing.ShouldDeclareNamespace(NamespaceOf<PublishedLayoutWriter>());
        SourceFileFor<PublishedLayoutWriter>("Publishing")
            .ShouldContainType(nameof(PublishedLayoutWriter), InternalAccessibility, true);
        SourceFileFor(typeof(PublishedBlockFacts), "Publishing")
            .ShouldContainType(nameof(PublishedBlockFacts), InternalAccessibility);

        publishing.ShouldNotUseNamespaces(
            NamespaceOf<FragmentBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            "Html2x.Renderers",
            ResourcesAssemblyName,
            ParserPackageName(),
            "Html2x.LayoutEngine.Geometry.Composition");
        publishing.ShouldNotUseIdentifiers(
            nameof(LayoutGeometryBuilder),
            nameof(GeometryPipelineComposer),
            nameof(FragmentBuilder),
            nameof(LayoutPaginator),
            "ImageResourceLoader");
    }

    [Fact]
    public void MeasurementOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        var measurement = CSharpSourceSet.FromDirectory("src", AssemblyName<LayoutGeometryBuilder>(), "Measurement");

        measurement.ShouldDeclareNamespace(NamespaceOf<BlockContentExtentMeasurement>());
        SourceFileFor<BlockContentExtentMeasurement>("Measurement")
            .ShouldContainType(nameof(BlockContentExtentMeasurement), InternalAccessibility, true);

        measurement.ShouldNotUseNamespaces(
            NamespaceOf<PublishedLayoutWriter>(),
            NamespaceOf<FragmentBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            "Html2x.Renderers",
            ResourcesAssemblyName,
            ParserPackageName(),
            "Html2x.LayoutEngine.Geometry.Composition");
        measurement.ShouldNotUseIdentifiers(
            nameof(LayoutGeometryBuilder),
            nameof(GeometryPipelineComposer),
            nameof(FragmentBuilder),
            nameof(LayoutPaginator),
            nameof(PublishedLayoutWriter),
            "ImageResourceLoader");
    }

    [Fact]
    public void InlineFlowOwner_UsesSharedRunAndBaselineRules()
    {
        SourceFileFor<InlineFlowLayout>("Box")
            .ShouldUseIdentifier(nameof(InlineRunCollection));
        SourceFileFor<AtomicInlineBoxLayout>("InlineFlow")
            .ShouldUseIdentifier(nameof(InlineRunCollection));
        SourceFileFor<InlineLayoutWriter>("InlineFlow")
            .ShouldUseIdentifier(nameof(InlineBaselineRules));
        SourceFileFor<AtomicInlineBoxLayout>("InlineFlow")
            .ShouldUseIdentifier(nameof(InlineBaselineRules));
    }

    [Fact]
    public void TablesOwner_HasExpectedNamespaceAndForbiddenDependencies()
    {
        ArchitectureDocument.Load("docs", "internals", "geometry.md")
            .ShouldMentionTopicsInSection(
                "Geometry Owners",
                "Html2x.LayoutEngine.Geometry.Tables",
                "Html2x.LayoutEngine.Geometry.Images",
                "TableGridDiagnostics",
                "Diagnostics",
                "Models");

        var tableSourceSet = CSharpSourceSet.FromDirectory("src", AssemblyName<LayoutGeometryBuilder>(), "Tables");
        tableSourceSet.ShouldDeclareNamespace(TablesNamespace);

        var tables = TableOwnerSources()
            .Select(static source => new
            {
                Source = CSharpSourceFile.Load(
                    "src",
                    AssemblyName<LayoutGeometryBuilder>(),
                    "Tables",
                    source.FileName),
                source.TypeName,
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
                "Html2x.LayoutEngine.Geometry.Publishing",
                NamespaceOf<FragmentBuilder>(),
                NamespaceOf<LayoutPaginator>(),
                "Html2x.Renderers",
                ResourcesAssemblyName,
                ParserPackageName(),
                "Html2x.LayoutEngine.Geometry.Composition");
            table.Source.ShouldNotUseIdentifiers(
                nameof(LayoutGeometryBuilder),
                nameof(GeometryPipelineComposer),
                nameof(FragmentBuilder),
                nameof(LayoutPaginator),
                nameof(PublishedLayoutWriter),
                "ImageResourceLoader");
        }

        var diagnostics = CSharpSourceFile.Load(
            "src",
            AssemblyName<LayoutGeometryBuilder>(),
            "Diagnostics",
            "TableGridDiagnostics.cs");
        diagnostics.ShouldDeclareNamespace(DiagnosticsNamespace);
        diagnostics.ShouldContainType("TableGridDiagnostics", InternalAccessibility);
    }

    private const string TablesNamespace = "Html2x.LayoutEngine.Geometry.Tables";

    private const string DiagnosticsNamespace = "Html2x.LayoutEngine.Geometry.Diagnostics";

    private static IReadOnlyList<TableOwnerSource> TableOwnerSources() =>
    [
        new("TableBlockLayout.cs", "TableBlockLayout"),
        new("TableBlockLayoutRule.cs", "TableBlockLayoutRule"),
        new("TableCellMeasurement.cs", "TableCellMeasurement"),
        new("TableGridLayout.cs", "TableGridLayout"),
        new("TableLayoutCellPlacement.cs", "TableLayoutCellPlacement"),
        new("TableLayoutResult.cs", "TableLayoutResult"),
        new("TableLayoutRowResult.cs", "TableLayoutRowResult"),
        new("TablePlacementWriter.cs", "TablePlacementWriter"),
        new("TableStructure.cs", "TableStructure"),
        new("TableStructureResult.cs", "TableStructureResult", true),
        new("TableStructureDiagnosticNames.cs", "TableStructureDiagnosticNames"),
        new("TableGridDiagnosticNames.cs", "TableGridDiagnosticNames"),
        new("TableRowDiagnosticFacts.cs", "TableRowDiagnosticFacts"),
        new("TableCellDiagnosticFacts.cs", "TableCellDiagnosticFacts"),
        new("TableColumnDiagnosticFacts.cs", "TableColumnDiagnosticFacts"),
        new("TableGroupDiagnosticFacts.cs", "TableGroupDiagnosticFacts")
    ];

    private sealed record TableOwnerSource(
        string FileName,
        string TypeName,
        bool IsRecordStruct = false);
}
