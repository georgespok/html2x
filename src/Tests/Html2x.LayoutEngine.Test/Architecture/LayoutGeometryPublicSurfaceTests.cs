using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Box.Publishing;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Style.Style;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Styles;
using Html2x.Text;
using Shouldly;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryPublicSurfaceTests
{
    [Fact]
    public void TextRuntimeAdapters_DoNotLeakThroughPublicConstructors()
    {
        var fontPathSource = SourceFileFor<FontPathSource>();
        var textMeasurer = SourceFileFor<SkiaTextMeasurer>();
        var renderer = CSharpSourceFile.Load("src", PdfRendererAssemblyName, "Pipeline", "PdfRenderer.cs");

        fontPathSource.ShouldContainConstructor(nameof(FontPathSource), PublicAccessibility);
        fontPathSource.ShouldNotHavePublicConstructorParameter("FontPathSource", "IFileDirectory");
        fontPathSource.ShouldNotHavePublicConstructorParameter("FontPathSource", "ISkiaTypefaceFactory");
        textMeasurer.ShouldContainConstructor(nameof(SkiaTextMeasurer), PublicAccessibility);
        textMeasurer.ShouldNotHavePublicConstructorParameter("SkiaTextMeasurer", "IFileDirectory");
        textMeasurer.ShouldNotHavePublicConstructorParameter("SkiaTextMeasurer", "ISkiaTypefaceFactory");
        renderer.ShouldContainConstructor("PdfRenderer", "public");
        renderer.ShouldNotHavePublicConstructorParameter("PdfRenderer", "IFileDirectory");
        renderer.ShouldNotHavePublicConstructorParameter("PdfRenderer", "ISkiaTypefaceFactory");
    }

    [Fact]
    public void TextImplementationHelpers_AreNotPublicSurface()
    {
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "FontDirectoryIndex.cs")
            .ShouldContainType("FontDirectoryIndex", "internal");
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "FontFaceEntry.cs")
            .ShouldContainType("FontFaceEntry", "internal");
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "FileDirectory.cs")
            .ShouldContainType("FileDirectory", "internal", true);
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "IFileDirectory.cs")
            .ShouldContainType("IFileDirectory", "internal");
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "SkiaTypefaceFactory.cs")
            .ShouldContainType("SkiaTypefaceFactory", "internal", true);
        CSharpSourceFile.Load("src", AssemblyName<FontPathSource>(), "ISkiaTypefaceFactory.cs")
            .ShouldContainType("ISkiaTypefaceFactory", "internal");
    }

    [Fact]
    public void PublicSurface_DoesNotExposeNonFacadeStageImplementations()
    {
        var layoutEnginePublic = SemanticProjectFor<LayoutBuilder>()
            .ExternallyVisibleTypeNames();
        var contractsPublic = SemanticProjectFor<StyleNode>()
            .ExternallyVisibleTypeNames();
        var stylePublic = SemanticProjectFor<StyleTreeBuilder>()
            .ExternallyVisibleTypeNames();
        var fragmentsPublic = SemanticProjectFor<FragmentBuilder>()
            .ExternallyVisibleTypeNames();
        var paginationPublic = SemanticProjectFor<LayoutPaginator>()
            .ExternallyVisibleTypeNames();
        var geometryPublic = SemanticProjectFor<LayoutGeometryBuilder>()
            .ExternallyVisibleTypeNames();

        layoutEnginePublic.ShouldBeEmpty();
        contractsPublic.ShouldBeEmpty();
        layoutEnginePublic.ShouldNotContain(FullTypeName<LayoutBuilder>());
        layoutEnginePublic.ShouldNotContain(FullTypeName(typeof(LayoutSnapshotMapper)));
        contractsPublic.ShouldNotContain(FullTypeName<ComputedStyle>());
        contractsPublic.ShouldNotContain(FullTypeName<StyleTree>());
        contractsPublic.ShouldNotContain(FullTypeName<LayoutGeometryRequest>());
        contractsPublic.ShouldNotContain(FullTypeName<IImageMetadataResolver>());
        stylePublic.ShouldNotContain(FullTypeName<StyleTreeBuilder>());
        stylePublic.ShouldNotContain(FullTypeName<CssStyleComputer>());
        fragmentsPublic.ShouldNotContain(FullTypeName<FragmentBuilder>());
        fragmentsPublic.ShouldNotContain("Html2x.LayoutEngine.Fragments.StyleConverter");
        paginationPublic.ShouldBeEmpty();
        geometryPublic.ShouldNotContain(FullTypeName<BlockBox>());
        geometryPublic.ShouldNotContain(FullTypeName<InlineBox>());
        geometryPublic.ShouldNotContain(FullTypeName<BlockBoxLayout>());
    }

    [Fact]
    public void GeometryPublicSurface_DoesNotExposeMutableBoxTypes()
    {
        SourceSetFor<LayoutGeometryBuilder>()
            .ShouldNotContainPublicTypes(nameof(BlockBox), nameof(InlineBox));

        var blockBoxLayout = SourceFileFor<BlockBoxLayout>("Box");
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockFlowLayout));
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockLayoutRuleSet));
        blockBoxLayout.ShouldUseIdentifier(nameof(PublishedLayoutWriter));
        blockBoxLayout.ShouldNotUseIdentifier("BuildTableRowFacts");
        blockBoxLayout.ShouldNotUseIdentifier("BuildTableCellFacts");

        var BlockSizingRules = SourceFileFor<BlockSizingRules>("Box");
        BlockSizingRules.ShouldUseIdentifier(nameof(BlockFlowMeasurement));

        var blockContentMeasurement = SourceFileFor<BlockContentExtentMeasurement>("Formatting");
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
        var styleComputer = SourceFileFor<CssStyleComputer>("Style");
        var borderMapper = SourceFileFor<BorderStyleMapper>("Style");

        color.ShouldNotUseIdentifier("FromCss");
        color.ShouldNotUseNamespace("System.Globalization");
        styleComputer.ShouldUseIdentifier(nameof(CssColorParser));
        borderMapper.ShouldUseIdentifier(nameof(CssColorParser));
    }

    [Fact]
    public void SourceIdentity_AssignmentAndSnapshotBoundaries_AreExplicit()
    {
        var styleTraversal = SourceFileFor<StyleTraversal>("Style");
        var boxTreeConstruction = SourceFileFor<BoxTreeConstruction>("Box");
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
            snapshot.ShouldNotUseNamespaces(NamespaceOf<StyleTreeBuilder>(), NamespaceOf<LayoutGeometryBuilder>());
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
        var styleTraversal = SourceFileFor<StyleTraversal>("Style");
        var constants = SourceFileFor(typeof(HtmlCssConstants), "Style");

        constants.ShouldContainPropertyInType(
            nameof(HtmlCssConstants),
            nameof(HtmlCssConstants.SupportedElementTags),
            "IReadOnlySet<string>",
            PublicAccessibility);
        styleTraversal.ShouldUseIdentifier(nameof(HtmlCssConstants.SupportedElementTags));
        styleTraversal.ShouldNotUseIdentifier("SupportedTags");
    }

    [Fact]
    public void FriendAssemblies_AreExplicitAndLimited()
    {
        CSharpSourceFile.Load("src", AssemblyName<LayoutGeometryBuilder>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(AssemblyName<LayoutBuilder>(), TestAssemblyNameFor<LayoutGeometryBuilder>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<LayoutBuilder>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(FacadeAssemblyName, TestAssemblyNameFor<LayoutGeometryBuilder>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<FragmentBuilder>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                AssemblyName<LayoutBuilder>(),
                TestAssemblyNameFor<FragmentBuilder>(),
                TestAssemblyNameFor<LayoutGeometryBuilder>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<LayoutGeometryRequest>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                FacadeAssemblyName,
                AssemblyName<LayoutBuilder>(),
                AssemblyName<FragmentBuilder>(),
                TestAssemblyNameFor<FragmentBuilder>(),
                AssemblyName<LayoutGeometryBuilder>(),
                TestAssemblyNameFor<LayoutGeometryBuilder>(),
                AssemblyName<LayoutPaginator>(),
                AssemblyName<StyleTreeBuilder>(),
                TestAssemblyNameFor<StyleTreeBuilder>(),
                CurrentAssemblyName());
        CSharpSourceFile.Load("src", AssemblyName<LayoutPaginator>(), "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                AssemblyName<LayoutBuilder>(),
                TestAssemblyNameFor<LayoutGeometryBuilder>(),
                TestAssemblyNameFor<LayoutPaginator>(),
                CurrentAssemblyName());
    }
}