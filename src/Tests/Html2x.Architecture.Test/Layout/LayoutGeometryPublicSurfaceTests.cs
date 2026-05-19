using System.Globalization;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine;
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
using Html2x.LayoutEngine.Stage.Contracts.Geometry;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Style.Computation;
using Html2x.Options;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.Resources;
using Html2x.Text;
using Shouldly;
using Html2x.Architecture.Test.Support;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test.Layout;

public sealed class LayoutGeometryPublicSurfaceTests
{
    [Fact]
    public void FacadePublicSurface_StaysNarrowAndStable()
    {
        SemanticProjectFor<HtmlConverter>()
            .ExternallyVisibleTypeNames()
            .ShouldBe([
                FullTypeName<HtmlConverter>(),
                FullTypeName<HtmlConverterDependencies>(),
                FullTypeName<HtmlToPdfResult>(),
                FullTypeName<CssOptions>(),
                FullTypeName<DiagnosticsOptions>(),
                FullTypeName<FontOptions>(),
                FullTypeName<HtmlConverterOptions>(),
                FullTypeName<PageOptions>(),
                FullTypeName<ResourceOptions>()
            ]);
    }

    [Fact]
    public void TextAdapterDependencies_DoNotLeakThroughPublicConstructors()
    {
        var fontPathSource = SourceFileFor<FontPathSource>();
        var textMeasurer = SourceFileFor<SkiaTextMeasurer>();
        var dependencies = SourceFileFor<HtmlConverterDependencies>();
        var renderer = SourceFileFor<PdfRenderer>();

        dependencies.ShouldContainPropertyInType(
            nameof(HtmlConverterDependencies),
            nameof(HtmlConverterDependencies.FontSourceFactory),
            NullableFuncTypeName<IFontSource>(),
            PublicAccessibility);
        dependencies.ShouldContainPropertyInType(
            nameof(HtmlConverterDependencies),
            nameof(HtmlConverterDependencies.TextMeasurerFactory),
            NullableFuncTypeName<ITextMeasurer>(),
            PublicAccessibility);
        dependencies.ShouldNotContainPropertyInType(
            nameof(HtmlConverterDependencies),
            DirectDependencyPropertyName(nameof(HtmlConverterDependencies.FontSourceFactory)));
        dependencies.ShouldNotContainPropertyInType(
            nameof(HtmlConverterDependencies),
            DirectDependencyPropertyName(nameof(HtmlConverterDependencies.TextMeasurerFactory)));
        fontPathSource.ShouldContainConstructor<FontPathSource>(PublicAccessibility);
        fontPathSource.ShouldNotHavePublicConstructorParameter<FontPathSource, IFileSystemReader>();
        fontPathSource.ShouldNotHavePublicConstructorParameter<FontPathSource, ISkiaTypefaceFactory>();
        textMeasurer.ShouldContainConstructor<SkiaTextMeasurer>(PublicAccessibility);
        textMeasurer.ShouldNotHavePublicConstructorParameter<SkiaTextMeasurer, IFileSystemReader>();
        textMeasurer.ShouldNotHavePublicConstructorParameter<SkiaTextMeasurer, ISkiaTypefaceFactory>();
        renderer.ShouldContainType<PdfRenderer>(InternalAccessibility, true);
        renderer.ShouldContainConstructor<PdfRenderer>(InternalAccessibility);
        renderer.ShouldNotHavePublicConstructorParameter<PdfRenderer, IFileSystemReader>();
        renderer.ShouldNotHavePublicConstructorParameter<PdfRenderer, ISkiaTypefaceFactory>();
    }

    [Fact]
    public void TextImplementationHelpers_AreNotPublicSurface()
    {
        SourceFileFor(typeof(FontDirectoryIndex))
            .ShouldContainType(typeof(FontDirectoryIndex), InternalAccessibility);
        SourceFileFor(typeof(FontFaceEntry))
            .ShouldContainType(typeof(FontFaceEntry), InternalAccessibility);
        SourceFileFor<FileSystemReader>()
            .ShouldContainType<FileSystemReader>(InternalAccessibility, true);
        SourceFileFor(typeof(IFileSystemReader))
            .ShouldContainType(typeof(IFileSystemReader), InternalAccessibility);
        SourceFileFor<SkiaTypefaceFactory>()
            .ShouldContainType<SkiaTypefaceFactory>(InternalAccessibility, true);
        SourceFileFor(typeof(ISkiaTypefaceFactory))
            .ShouldContainType(typeof(ISkiaTypefaceFactory), InternalAccessibility);
        SourceFileFor<DiagnosticsFontSource>()
            .ShouldContainType<DiagnosticsFontSource>(InternalAccessibility, true);
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
        var stageContractsPublic = SemanticProjectFor<ILayoutGeometryStage>()
            .ExternallyVisibleTypeNames();
        var fragmentsPublic = SemanticProjectFor<FragmentTreeBuilder>()
            .ExternallyVisibleTypeNames();
        var paginationPublic = SemanticProjectFor<LayoutPaginator>()
            .ExternallyVisibleTypeNames();
        var geometryPublic = SemanticProjectFor<LayoutGeometryConstruction>()
            .ExternallyVisibleTypeNames();
        var rendererPublic = SemanticProjectFor<PdfRenderer>()
            .ExternallyVisibleTypeNames();

        layoutEnginePublic.ShouldBeEmpty();
        contractsPublic.ShouldBeEmpty();
        stageContractsPublic.ShouldBeEmpty();
        layoutEnginePublic.ShouldNotContain(FullTypeName<LayoutPipeline>());
        layoutEnginePublic.ShouldNotContain(FullTypeName(typeof(LayoutSnapshotMapper)));
        contractsPublic.ShouldNotContain(FullTypeName<ComputedStyle>());
        contractsPublic.ShouldNotContain(FullTypeName<StyleTree>());
        contractsPublic.ShouldNotContain(FullTypeName<LayoutGeometryRequest>());
        contractsPublic.ShouldNotContain(FullTypeName<IImageMetadataResolver>());
        stylePublic.ShouldNotContain(FullTypeName<StyleTreeBuilder>());
        stylePublic.ShouldNotContain(FullTypeName<CssStyleComputer>());
        fragmentsPublic.ShouldNotContain(FullTypeName<FragmentTreeBuilder>());
        paginationPublic.ShouldBeEmpty();
        rendererPublic.ShouldNotContain(FullTypeName<PdfRenderer>());
        rendererPublic.ShouldNotContain(FullTypeName<PdfRenderSettings>());
        geometryPublic.ShouldNotContain(FullTypeName<BlockBox>());
        geometryPublic.ShouldNotContain(FullTypeName<InlineBox>());
        geometryPublic.ShouldNotContain(FullTypeName<BlockBoxLayout>());
    }

    [Fact]
    public void RenderModelPublicSurface_DoesNotExposeDocumentsOrFragments()
    {
        var publicTypes = SemanticProjectFor<HtmlLayout>()
            .ExternallyVisibleTypeNames();
        var textDecorations = SourceFileFor<TextDecorations>();

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
        textDecorations.ShouldDeclareNamespace(NamespaceOf<VisualStyle>());
        textDecorations.ShouldNotDeclareNamespace(NamespaceOf<Fragment>());
    }

    [Fact]
    public void GeometryPublicSurface_DoesNotExposeMutableBoxTypes()
    {
        SourceSetFor<LayoutGeometryConstruction>()
            .ShouldNotContainPublicTypes(nameof(BlockBox), nameof(InlineBox));

        var blockBoxLayout = SourceFileFor<BlockBoxLayout>();
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockFlowLayout));
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockLayoutRuleSet));
        blockBoxLayout.ShouldUseIdentifier(nameof(PublishedLayoutWriter));

        var blockSizingRules = SourceFileFor<BlockSizingRules>();
        blockSizingRules.ShouldUseIdentifier(nameof(BlockFlowMeasurement));

        var blockContentMeasurement = SourceFileFor<BlockFormattingMetricsMeasurement>();
        blockContentMeasurement.ShouldUseIdentifier(nameof(BlockFlowMeasurement));
    }

    [Fact]
    public void PublishedGeometryFacts_AvoidMutableBoxImplementationNamespace()
    {
        SourceSetForNamespaceOf<PublishedLayoutTree>()
            .ShouldNotUseNamespaces(NamespaceOf<BlockBox>(), NamespaceOf<BlockBoxLayout>());
        SourceSetFor<HtmlLayout>()
            .ShouldNotUseNamespaces(NamespaceOf<BlockBox>(), NamespaceOf<BlockBoxLayout>());
        SourceFileFor(typeof(GeometrySnapshotMapper))
            .ShouldNotUseIdentifier(nameof(BoxTreeConstruction).Replace("Construction", string.Empty,
                StringComparison.Ordinal));
    }

    [Fact]
    public void HtmlLayoutPages_AreReadOnlyAtRendererBoundary()
    {
        var htmlLayout = SourceFileFor<HtmlLayout>();
        var paginator = SourceFileFor<LayoutPaginator>();

        htmlLayout.ShouldContainPropertyInType<HtmlLayout>(nameof(HtmlLayout.Pages),
            ReadOnlyListTypeName<LayoutPage>(), PublicAccessibility);
        htmlLayout.ShouldContainMethodInType<HtmlLayout>(nameof(HtmlLayout.AddPage), VoidTypeName,
            PublicAccessibility);
        htmlLayout.ShouldNotContainPropertyInType<HtmlLayout>(nameof(HtmlLayout.Pages),
            InterfaceListTypeName<LayoutPage>(), PublicAccessibility);
        paginator.ShouldInvoke(nameof(HtmlLayout.AddPage));
    }

    [Fact]
    public void RenderModelColorFacts_DoNotOwnCssParsing()
    {
        var color = SourceFileFor<ColorRgba>();
        var styleComputer = SourceFileFor<CssStyleComputer>();
        var borderMapper = SourceFileFor<BorderStyleMapper>();

        color.ShouldNotUseNamespace(NamespaceOf<CultureInfo>());
        styleComputer.ShouldUseIdentifier(nameof(CssColorParser));
        borderMapper.ShouldUseIdentifier(nameof(CssColorParser));
    }

    [Fact]
    public void SourceIdentity_AssignmentAndSnapshotBoundaries_AreExplicit()
    {
        var styleTraversal = SourceFileFor<StyleTraversal>();
        var boxTreeConstruction = SourceFileFor<BoxTreeConstruction>();
        var snapshots = new[]
        {
            SourceFileFor<LayoutSnapshot>(),
            SourceFileFor<LayoutPageSnapshot>(),
            SourceFileFor<FragmentSnapshot>(),
            SourceFileFor<GeometrySnapshot>(),
            SourceFileFor<BoxGeometrySnapshot>(),
            SourceFileFor<PaginationPageSnapshot>(),
            SourceFileFor<PaginationPlacementSnapshot>()
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

        boxGeometrySnapshot.ShouldContainPropertyInType<BoxGeometrySnapshot>(
            nameof(BoxGeometrySnapshot.SourceNodeId), NullableCSharpTypeName<int>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType<BoxGeometrySnapshot>(
            nameof(BoxGeometrySnapshot.SourceContentId), NullableCSharpTypeName<int>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType<BoxGeometrySnapshot>(
            nameof(BoxGeometrySnapshot.SourcePath), NullableCSharpTypeName<string>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType<BoxGeometrySnapshot>(
            nameof(BoxGeometrySnapshot.SourceOrder), NullableCSharpTypeName<int>(), PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType<BoxGeometrySnapshot>(
            nameof(BoxGeometrySnapshot.SourceElementIdentity),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);
        boxGeometrySnapshot.ShouldContainPropertyInType<BoxGeometrySnapshot>(
            nameof(BoxGeometrySnapshot.GeneratedSourceKind),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);
    }

    [Fact]
    public void SupportedElementTraversal_IsOwnedByStyle()
    {
        var styleTraversal = SourceFileFor<StyleTraversal>();
        var constants = SourceFileFor(typeof(HtmlCssVocabulary));
        var supportedElementRules = SourceFileFor(typeof(SupportedElementRules));

        constants.ShouldNotContainPropertyInType(
            nameof(HtmlCssVocabulary),
            "SupportedElementTags",
            null,
            PublicAccessibility);
        supportedElementRules.ShouldDeclareNamespace(NamespaceOf<StyleTraversal>());
        supportedElementRules.ShouldUseIdentifier(nameof(HtmlCssVocabulary.HtmlTags));
        styleTraversal.ShouldUseIdentifier(nameof(SupportedElementRules));
        styleTraversal.ShouldNotUseIdentifier("SupportedElementTags");
    }

    [Fact]
    public void FriendAssemblies_AreExplicitAndLimited()
    {
        CompiledAssembly.For<HtmlConverter>()
            .ShouldHaveFriendAssemblies(CurrentAssemblyName());
        CompiledAssembly.For<IDiagnosticsSink>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<StyleTreeBuilder>(),
                CurrentAssemblyName());
        CompiledAssembly.For<LayoutGeometryConstruction>()
            .ShouldHaveFriendAssemblies(AssemblyName<LayoutPipeline>(), TestAssemblyNameFor<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutPipeline>(), CurrentAssemblyName());
        CompiledAssembly.For<LayoutPipeline>()
            .ShouldHaveFriendAssemblies(AssemblyName<HtmlConverter>(), TestAssemblyNameFor<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutPipeline>(), CurrentAssemblyName());
        CompiledAssembly.For<FragmentTreeBuilder>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<LayoutPipeline>(),
                TestAssemblyNameFor<FragmentTreeBuilder>(),
                TestAssemblyNameFor<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutPipeline>(),
                CurrentAssemblyName());
        CompiledAssembly.For<LayoutGeometryRequest>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<HtmlConverter>(),
                AssemblyName<LayoutPipeline>(),
                AssemblyName<FragmentTreeBuilder>(),
                TestAssemblyNameFor<FragmentTreeBuilder>(),
                AssemblyName<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutGeometryConstruction>(),
                AssemblyName<LayoutPaginator>(),
                AssemblyName<ILayoutGeometryStage>(),
                AssemblyName<StyleTreeBuilder>(),
                TestAssemblyNameFor<StyleTreeBuilder>(),
                TestAssemblyNameFor<LayoutPipeline>(),
                CurrentAssemblyName());
        CompiledAssembly.For<ILayoutGeometryStage>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutPipeline>(),
                CurrentAssemblyName());
        CompiledAssembly.For<LayoutPaginator>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<LayoutPipeline>(),
                TestAssemblyNameFor<LayoutGeometryConstruction>(),
                TestAssemblyNameFor<LayoutPaginator>(),
                TestAssemblyNameFor<LayoutPipeline>(),
                CurrentAssemblyName());
        CompiledAssembly.For<PdfRenderer>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<HtmlConverter>(),
                TestAssemblyNameFor<PdfRenderer>(),
                CurrentAssemblyName());
        CompiledAssembly.For<FontPathSource>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<HtmlConverter>(),
                AssemblyName<PdfRenderer>(),
                TestAssemblyNameFor<PdfRenderer>(),
                CurrentAssemblyName());
        CompiledAssembly.For<ImageResourceStore>()
            .ShouldHaveFriendAssemblies(
                AssemblyName<HtmlConverter>(),
                TestAssemblyNameFor<HtmlConverter>(),
                AssemblyName<PdfRenderer>(),
                TestAssemblyNameFor<PdfRenderer>(),
                CurrentAssemblyName());
    }

    private static string DirectDependencyPropertyName(string factoryPropertyName) =>
        factoryPropertyName.Replace("Factory", string.Empty, StringComparison.Ordinal);
}
