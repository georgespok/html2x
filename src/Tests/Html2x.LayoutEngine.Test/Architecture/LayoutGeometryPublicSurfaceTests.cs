using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Construction;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Publishing;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Style.Computation;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.Text;
using Shouldly;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryPublicSurfaceTests
{
    [Fact]
    public void TextAdapterDependencies_DoNotLeakThroughPublicConstructors()
    {
        var fontPathSource = SourceFileFor<FontPathSource>();
        var textMeasurer = SourceFileFor<SkiaTextMeasurer>();
        var dependencies = CSharpSourceFile.Load("src", FacadeAssemblyName, "HtmlConverterDependencies.cs");
        var renderer = CSharpSourceFile.Load("src", PdfRendererAssemblyName, "Pipeline", "PdfRenderer.cs");

        dependencies.ShouldContainPropertyInType("HtmlConverterDependencies", "FontSourceFactory", "Func<IFontSource>?",
            PublicAccessibility);
        dependencies.ShouldContainPropertyInType("HtmlConverterDependencies", "TextMeasurerFactory", "Func<ITextMeasurer>?",
            PublicAccessibility);
        dependencies.ShouldNotContainPropertyInType("HtmlConverterDependencies", "FontSource");
        dependencies.ShouldNotContainPropertyInType("HtmlConverterDependencies", "TextMeasurer");
        fontPathSource.ShouldContainConstructor(nameof(FontPathSource), PublicAccessibility);
        fontPathSource.ShouldNotHavePublicConstructorParameter("FontPathSource", "IFileSystemReader");
        fontPathSource.ShouldNotHavePublicConstructorParameter("FontPathSource", "ISkiaTypefaceFactory");
        textMeasurer.ShouldContainConstructor(nameof(SkiaTextMeasurer), PublicAccessibility);
        textMeasurer.ShouldNotHavePublicConstructorParameter("SkiaTextMeasurer", "IFileSystemReader");
        textMeasurer.ShouldNotHavePublicConstructorParameter("SkiaTextMeasurer", "ISkiaTypefaceFactory");
        renderer.ShouldContainType("PdfRenderer", InternalAccessibility, true);
        renderer.ShouldContainConstructor("PdfRenderer", InternalAccessibility);
        renderer.ShouldNotHavePublicConstructorParameter("PdfRenderer", "IFileSystemReader");
        renderer.ShouldNotHavePublicConstructorParameter("PdfRenderer", "ISkiaTypefaceFactory");
    }

    [Fact]
    public void TextImplementationHelpers_AreNotPublicSurface()
    {
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "FontDirectoryIndex.cs")
            .ShouldContainType("FontDirectoryIndex", "internal");
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "FontFaceEntry.cs")
            .ShouldContainType("FontFaceEntry", "internal");
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "FileSystemReader.cs")
            .ShouldContainType("FileSystemReader", "internal", true);
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "IFileSystemReader.cs")
            .ShouldContainType("IFileSystemReader", "internal");
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "SkiaTypefaceFactory.cs")
            .ShouldContainType("SkiaTypefaceFactory", "internal", true);
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "ISkiaTypefaceFactory.cs")
            .ShouldContainType("ISkiaTypefaceFactory", "internal");
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "DiagnosticsFontSource.cs")
            .ShouldContainType("DiagnosticsFontSource", "internal", true);
    }

    [Fact]
    public void PublicSurface_DoesNotExposeNonFacadeStageImplementations()
    {
        var layoutEnginePublic = SemanticProjectFor<LayoutPipeline>()
            .ExternallyVisibleTypeNames();
        var contractsPublic = SemanticProjectFor<StyleNode>()
            .ExternallyVisibleTypeNames();
        var stylePublic = SemanticProjectFor<StyleTreeBuilder>()
            .ExternallyVisibleTypeNames();
        var fragmentsPublic = SemanticProjectFor<FragmentTreeBuilder>()
            .ExternallyVisibleTypeNames();
        var paginationPublic = SemanticProjectFor<LayoutPaginator>()
            .ExternallyVisibleTypeNames();
        var geometryPublic = SemanticProjectFor<LayoutGeometryConstruction>()
            .ExternallyVisibleTypeNames();
        var rendererPublic = ArchitectureSemanticProject.Load("src", PdfRendererAssemblyName, PdfRendererAssemblyName + ".csproj")
            .ExternallyVisibleTypeNames();

        layoutEnginePublic.ShouldBeEmpty();
        contractsPublic.ShouldBeEmpty();
        layoutEnginePublic.ShouldNotContain(FullTypeName<LayoutPipeline>());
        layoutEnginePublic.ShouldNotContain(FullTypeName(typeof(LayoutSnapshotMapper)));
        contractsPublic.ShouldNotContain(FullTypeName<ComputedStyle>());
        contractsPublic.ShouldNotContain(FullTypeName<StyleTree>());
        contractsPublic.ShouldNotContain(FullTypeName<LayoutGeometryRequest>());
        contractsPublic.ShouldNotContain(FullTypeName<IImageMetadataResolver>());
        stylePublic.ShouldNotContain(FullTypeName<StyleTreeBuilder>());
        stylePublic.ShouldNotContain(FullTypeName<CssStyleComputer>());
        fragmentsPublic.ShouldNotContain(FullTypeName<FragmentTreeBuilder>());
        fragmentsPublic.ShouldNotContain("Html2x.LayoutEngine.Fragments.StyleConverter");
        paginationPublic.ShouldBeEmpty();
        rendererPublic.ShouldNotContain("Html2x.Renderers.Pdf.Pipeline.PdfRenderer");
        rendererPublic.ShouldNotContain("Html2x.Renderers.Pdf.PdfRenderSettings");
        geometryPublic.ShouldNotContain(FullTypeName<BlockBox>());
        geometryPublic.ShouldNotContain(FullTypeName<InlineBox>());
        geometryPublic.ShouldNotContain(FullTypeName<BlockBoxLayout>());
    }

    [Fact]
    public void RenderModelPublicSurface_DoesNotExposeDocumentsOrFragments()
    {
        var publicTypes = SemanticProjectFor<HtmlLayout>()
            .ExternallyVisibleTypeNames();

        publicTypes.ShouldNotContain(FullTypeName<HtmlLayout>());
        publicTypes.ShouldNotContain(FullTypeName<LayoutPage>());
        publicTypes.ShouldNotContain(FullTypeName<Fragment>());
        publicTypes.ShouldNotContain(FullTypeName<BlockFragment>());
        publicTypes.ShouldNotContain(FullTypeName<LineBoxFragment>());
        publicTypes.ShouldNotContain(FullTypeName<ImageFragment>());
        publicTypes.ShouldNotContain(FullTypeName<RuleFragment>());
        publicTypes.ShouldNotContain(FullTypeName<TableFragment>());
        publicTypes.ShouldNotContain(FullTypeName<TableRowFragment>());
        publicTypes.ShouldNotContain(FullTypeName<TableCellFragment>());
        publicTypes.ShouldNotContain(FullTypeName<TextRun>());
        publicTypes.ShouldNotContain(FullTypeName<FragmentDisplayRole>());
        publicTypes.ShouldNotContain(FullTypeName<FormattingContextKind>());
        publicTypes.ShouldNotContain(FullTypeName<TextDecorations>());
    }

    [Fact]
    public void GeometryPublicSurface_DoesNotExposeMutableBoxTypes()
    {
        SourceSetFor<LayoutGeometryConstruction>()
            .ShouldNotContainPublicTypes(nameof(BlockBox), nameof(InlineBox));

        var blockBoxLayout = SourceFileFor<BlockBoxLayout>("BlockFlow");
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockFlowLayout));
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockLayoutRuleSet));
        blockBoxLayout.ShouldUseIdentifier(nameof(PublishedLayoutWriter));
        blockBoxLayout.ShouldNotUseIdentifier("BuildTableRowFacts");
        blockBoxLayout.ShouldNotUseIdentifier("BuildTableCellFacts");

        var BlockSizingRules = SourceFileFor<BlockSizingRules>("BlockFlow");
        BlockSizingRules.ShouldUseIdentifier(nameof(BlockFlowMeasurement));

        var blockContentMeasurement = SourceFileFor<BlockFormattingMetricsMeasurement>("Measurement");
        blockContentMeasurement.ShouldUseIdentifier(nameof(BlockFlowMeasurement));
    }

    [Fact]
    public void PublishedGeometryFacts_AvoidMutableBoxImplementationNamespace()
    {
        CSharpSourceSet.FromDirectory("src", AssemblyName<PublishedLayoutTree>(), "Published")
            .ShouldNotUseNamespaces(NamespaceOf<BlockBox>(), NamespaceOf<BlockBoxLayout>());
        SourceSetFor<HtmlLayout>()
            .ShouldNotUseNamespaces(NamespaceOf<BlockBox>(), NamespaceOf<BlockBoxLayout>());
        SourceFileFor(typeof(GeometrySnapshotMapper), "Diagnostics")
            .ShouldNotUseIdentifier("BoxTree");
    }

    [Fact]
    public void HtmlLayoutPages_AreReadOnlyAtRendererBoundary()
    {
        var htmlLayout = SourceFileFor<HtmlLayout>("Documents");
        var paginator = SourceFileFor<LayoutPaginator>();

        htmlLayout.ShouldContainPropertyInType(nameof(HtmlLayout), nameof(HtmlLayout.Pages),
            ReadOnlyListTypeName<LayoutPage>(), PublicAccessibility);
        htmlLayout.ShouldContainMethodInType(nameof(HtmlLayout), nameof(HtmlLayout.AddPage), VoidTypeName,
            PublicAccessibility);
        htmlLayout.ShouldNotContainPropertyInType(nameof(HtmlLayout), nameof(HtmlLayout.Pages),
            "IList<" + TypeName<LayoutPage>() + ">", PublicAccessibility);
        paginator.ShouldInvoke(nameof(HtmlLayout.AddPage));
    }

    [Fact]
    public void RenderModelColorFacts_DoNotOwnCssParsing()
    {
        var color = SourceFileFor<ColorRgba>("Styles");
        var styleComputer = SourceFileFor<CssStyleComputer>("Computation");
        var borderMapper = SourceFileFor<BorderStyleMapper>("Computation");

        color.ShouldNotUseIdentifier("FromCss");
        color.ShouldNotUseNamespace("System.Globalization");
        styleComputer.ShouldUseIdentifier(nameof(CssColorParser));
        borderMapper.ShouldUseIdentifier(nameof(CssColorParser));
    }

    [Fact]
    public void SourceIdentity_AssignmentAndSnapshotBoundaries_AreExplicit()
    {
        var styleTraversal = SourceFileFor<StyleTraversal>("Computation");
        var boxTreeConstruction = SourceFileFor<BoxTreeConstruction>("Construction");
        var snapshots = new[]
        {
            SourceFileFor<LayoutSnapshot>("Diagnostics"),
            SourceFileFor<LayoutPageSnapshot>("Diagnostics"),
            SourceFileFor<FragmentSnapshot>("Diagnostics"),
            SourceFileFor<GeometrySnapshot>("Diagnostics"),
            SourceFileFor<BoxGeometrySnapshot>("Diagnostics"),
            SourceFileFor<PaginationPageSnapshot>("Diagnostics"),
            SourceFileFor<PaginationPlacementSnapshot>("Diagnostics")
        };
        var boxGeometrySnapshot = snapshots[4];

        styleTraversal.ShouldConstructType(nameof(StyleSourceIdentity));
        styleTraversal.ShouldConstructType(nameof(StyleContentIdentity));
        boxTreeConstruction.ShouldUseIdentifier(nameof(StyleNode.Identity));
        boxTreeConstruction.ShouldUseIdentifier(nameof(StyleContentNode.Identity));
        boxTreeConstruction.ShouldUseIdentifier(nameof(GeometrySourceIdentity));
        boxTreeConstruction.ShouldUseIdentifier(nameof(GeometryGeneratedSourceKind.InlineBlockContent));
        boxTreeConstruction.ShouldUseIdentifier(nameof(GeometryGeneratedSourceKind.AnonymousText));
        boxTreeConstruction.ShouldNotConstructType(nameof(StyleSourceIdentity));
        boxTreeConstruction.ShouldNotConstructType(nameof(StyleContentIdentity));

        foreach (var snapshot in snapshots)
        {
            snapshot.ShouldNotUseNamespaces(NamespaceOf<StyleTreeBuilder>(), NamespaceOf<LayoutGeometryConstruction>());
        }

        boxGeometrySnapshot.ShouldContainPropertyInType(nameof(BoxGeometrySnapshot),
            nameof(BoxGeometrySnapshot.SourceNodeId), NullableCSharpTypeName<int>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType(nameof(BoxGeometrySnapshot),
            nameof(BoxGeometrySnapshot.SourceContentId), NullableCSharpTypeName<int>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType(nameof(BoxGeometrySnapshot),
            nameof(BoxGeometrySnapshot.SourcePath), NullableCSharpTypeName<string>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType(nameof(BoxGeometrySnapshot),
            nameof(BoxGeometrySnapshot.SourceOrder), NullableCSharpTypeName<int>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType(
            nameof(BoxGeometrySnapshot),
            nameof(BoxGeometrySnapshot.SourceElementIdentity),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType(
            nameof(BoxGeometrySnapshot),
            nameof(BoxGeometrySnapshot.GeneratedSourceKind),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);
    }

    [Fact]
    public void SupportedHtmlVocabulary_HasSingleStyleContractOwner()
    {
        var styleTraversal = SourceFileFor<StyleTraversal>("Computation");
        var constants = SourceFileFor(typeof(HtmlCssVocabulary), "Style");

        constants.ShouldContainPropertyInType(
            nameof(HtmlCssVocabulary),
            nameof(HtmlCssVocabulary.SupportedElementTags),
            "IReadOnlySet<string>",
            PublicAccessibility);
        styleTraversal.ShouldUseIdentifier(nameof(HtmlCssVocabulary.SupportedElementTags));
        styleTraversal.ShouldNotUseIdentifier("SupportedTags");
    }

    [Fact]
    public void FriendAssemblies_AreExplicitAndLimited()
    {
        CSharpSourceFile.Load("src", AssemblyName<LayoutGeometryConstruction>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(AssemblyName<LayoutPipeline>(), TestAssemblyNameFor<LayoutGeometryConstruction>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<LayoutPipeline>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(FacadeAssemblyName, TestAssemblyNameFor<LayoutGeometryConstruction>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<FragmentTreeBuilder>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                AssemblyName<LayoutPipeline>(),
                TestAssemblyNameFor<FragmentTreeBuilder>(),
                TestAssemblyNameFor<LayoutGeometryConstruction>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<LayoutGeometryRequest>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                FacadeAssemblyName,
                AssemblyName<LayoutPipeline>(),
                AssemblyName<FragmentTreeBuilder>(),
                TestAssemblyNameFor<FragmentTreeBuilder>(),
                AssemblyName<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutGeometryConstruction>(),
                AssemblyName<LayoutPaginator>(),
                AssemblyName<StyleTreeBuilder>(),
                TestAssemblyNameFor<StyleTreeBuilder>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<LayoutPaginator>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                AssemblyName<LayoutPipeline>(),
                TestAssemblyNameFor<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutPaginator>(),
                CurrentAssemblyName());
    }
}
