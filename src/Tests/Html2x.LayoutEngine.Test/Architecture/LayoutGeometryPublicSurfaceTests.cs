using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryPublicSurfaceTests
{
    [Fact]
    public void TextRuntimeAdapters_DoNotLeakThroughPublicConstructors()
    {
        var fontPathSource = CSharpSourceFile.Load("src", "Html2x.Text", "FontPathSource.cs");
        var textMeasurer = CSharpSourceFile.Load("src", "Html2x.Text", "SkiaTextMeasurer.cs");
        var renderer = CSharpSourceFile.Load("src", "Html2x.Renderers.Pdf", "Pipeline", "PdfRenderer.cs");

        fontPathSource.ShouldContainConstructor("FontPathSource", "public");
        fontPathSource.ShouldNotHavePublicConstructorParameter("FontPathSource", "IFileDirectory");
        fontPathSource.ShouldNotHavePublicConstructorParameter("FontPathSource", "ISkiaTypefaceFactory");
        textMeasurer.ShouldContainConstructor("SkiaTextMeasurer", "public");
        textMeasurer.ShouldNotHavePublicConstructorParameter("SkiaTextMeasurer", "IFileDirectory");
        textMeasurer.ShouldNotHavePublicConstructorParameter("SkiaTextMeasurer", "ISkiaTypefaceFactory");
        renderer.ShouldContainConstructor("PdfRenderer", "public");
        renderer.ShouldNotHavePublicConstructorParameter("PdfRenderer", "IFileDirectory");
        renderer.ShouldNotHavePublicConstructorParameter("PdfRenderer", "ISkiaTypefaceFactory");
    }

    [Fact]
    public void TextImplementationHelpers_AreNotPublicSurface()
    {
        CSharpSourceFile.Load("src", "Html2x.Text", "FontDirectoryIndex.cs")
            .ShouldContainType("FontDirectoryIndex", "internal");
        CSharpSourceFile.Load("src", "Html2x.Text", "FontFaceEntry.cs")
            .ShouldContainType("FontFaceEntry", "internal");
        CSharpSourceFile.Load("src", "Html2x.Text", "FileDirectory.cs")
            .ShouldContainType("FileDirectory", "internal", isSealed: true);
        CSharpSourceFile.Load("src", "Html2x.Text", "IFileDirectory.cs")
            .ShouldContainType("IFileDirectory", "internal");
        CSharpSourceFile.Load("src", "Html2x.Text", "SkiaTypefaceFactory.cs")
            .ShouldContainType("SkiaTypefaceFactory", "internal", isSealed: true);
        CSharpSourceFile.Load("src", "Html2x.Text", "ISkiaTypefaceFactory.cs")
            .ShouldContainType("ISkiaTypefaceFactory", "internal");
    }

    [Fact]
    public void PublicSurface_DoesNotExposeNonFacadeStageImplementations()
    {
        var layoutEnginePublic = ArchitectureSemanticProject
            .Load("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj")
            .ExternallyVisibleTypeNames();
        var contractsPublic = ArchitectureSemanticProject
            .Load("src", "Html2x.LayoutEngine.Contracts", "Html2x.LayoutEngine.Contracts.csproj")
            .ExternallyVisibleTypeNames();
        var stylePublic = ArchitectureSemanticProject
            .Load("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj")
            .ExternallyVisibleTypeNames();
        var fragmentsPublic = ArchitectureSemanticProject
            .Load("src", "Html2x.LayoutEngine.Fragments", "Html2x.LayoutEngine.Fragments.csproj")
            .ExternallyVisibleTypeNames();
        var paginationPublic = ArchitectureSemanticProject
            .Load("src", "Html2x.LayoutEngine.Pagination", "Html2x.LayoutEngine.Pagination.csproj")
            .ExternallyVisibleTypeNames();
        var geometryPublic = ArchitectureSemanticProject
            .Load("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj")
            .ExternallyVisibleTypeNames();

        layoutEnginePublic.ShouldBeEmpty();
        contractsPublic.ShouldBeEmpty();
        layoutEnginePublic.ShouldNotContain("Html2x.LayoutEngine.LayoutBuilder");
        layoutEnginePublic.ShouldNotContain("Html2x.LayoutEngine.Diagnostics.LayoutSnapshotMapper");
        contractsPublic.ShouldNotContain("Html2x.LayoutEngine.Contracts.Style.ComputedStyle");
        contractsPublic.ShouldNotContain("Html2x.LayoutEngine.Contracts.Style.StyleTree");
        contractsPublic.ShouldNotContain("Html2x.LayoutEngine.Contracts.Geometry.LayoutGeometryRequest");
        contractsPublic.ShouldNotContain("Html2x.LayoutEngine.Contracts.Geometry.Images.IImageMetadataResolver");
        stylePublic.ShouldNotContain("Html2x.LayoutEngine.Style.StyleTreeBuilder");
        stylePublic.ShouldNotContain("Html2x.LayoutEngine.Style.CssStyleComputer");
        fragmentsPublic.ShouldNotContain("Html2x.LayoutEngine.Fragments.FragmentBuilder");
        fragmentsPublic.ShouldNotContain("Html2x.LayoutEngine.Fragments.StyleConverter");
        paginationPublic.ShouldBeEmpty();
        geometryPublic.ShouldNotContain("Html2x.LayoutEngine.Geometry.Models.BlockBox");
        geometryPublic.ShouldNotContain("Html2x.LayoutEngine.Geometry.Models.InlineBox");
        geometryPublic.ShouldNotContain("Html2x.LayoutEngine.Geometry.Box.BlockLayoutEngine");
    }

    [Fact]
    public void GeometryPublicSurface_DoesNotExposeMutableBoxTypes()
    {
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Geometry")
            .ShouldNotContainPublicTypes("BlockBox", "InlineBox");

        var blockLayoutEngine = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Geometry", "Box", "BlockLayoutEngine.cs");
        blockLayoutEngine.ShouldUseIdentifier("BlockFlowLayoutExecutor");
        blockLayoutEngine.ShouldNotUseIdentifier("BuildTableRowContexts");
        blockLayoutEngine.ShouldNotUseIdentifier("BuildTableCellContexts");

        var blockMeasurementService = CSharpSourceFile.Load(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Box",
            "BlockMeasurementService.cs");
        blockMeasurementService.ShouldUseIdentifier("BlockFlowMeasurementExecutor");

        var blockFormattingContext = CSharpSourceFile.Load(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Formatting",
            "BlockFormattingContext.cs");
        blockFormattingContext.ShouldUseIdentifier("BlockFlowMeasurementExecutor");
    }

    [Fact]
    public void PublishedGeometryFacts_AvoidMutableBoxImplementationNamespace()
    {
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Contracts", "Published")
            .ShouldNotUseNamespaces("Html2x.LayoutEngine.Geometry.Models", "Html2x.LayoutEngine.Box");
        CSharpSourceSet.FromDirectory("src", "Html2x.RenderModel")
            .ShouldNotUseNamespaces("Html2x.LayoutEngine.Geometry.Models", "Html2x.LayoutEngine.Box");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "GeometrySnapshotMapper.cs")
            .ShouldNotUseIdentifier("BoxTree");
    }

    [Fact]
    public void HtmlLayoutPages_AreReadOnlyAtRendererBoundary()
    {
        var htmlLayout = CSharpSourceFile.Load("src", "Html2x.RenderModel", "Documents", "HtmlLayout.cs");
        var paginator = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Pagination", "LayoutPaginator.cs");

        htmlLayout.ShouldContainPropertyInType("HtmlLayout", "Pages", "IReadOnlyList<LayoutPage>", "public");
        htmlLayout.ShouldContainMethodInType("HtmlLayout", "AddPage", "void", "public");
        htmlLayout.ShouldNotContainPropertyInType("HtmlLayout", "Pages", "IList<LayoutPage>", "public");
        paginator.ShouldInvoke("AddPage");
    }

    [Fact]
    public void SourceIdentity_AssignmentAndSnapshotBoundaries_AreExplicit()
    {
        var styleTraversal = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Style", "Style", "StyleTraversal.cs");
        var initialBoxTreeBuilder = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Geometry", "Box", "InitialBoxTreeBuilder.cs");
        var snapshots = new[]
        {
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "LayoutSnapshot.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "LayoutPageSnapshot.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "FragmentSnapshot.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "GeometrySnapshot.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "BoxGeometrySnapshot.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "PaginationPageSnapshot.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Diagnostics", "PaginationPlacementSnapshot.cs")
        };
        var boxGeometrySnapshot = snapshots[4];

        styleTraversal.ShouldConstructType("StyleSourceIdentity");
        styleTraversal.ShouldConstructType("StyleContentIdentity");
        initialBoxTreeBuilder.ShouldUseIdentifier("Identity");
        initialBoxTreeBuilder.ShouldNotConstructType("StyleSourceIdentity");
        initialBoxTreeBuilder.ShouldNotConstructType("StyleContentIdentity");
        foreach (var snapshot in snapshots)
        {
            snapshot.ShouldNotUseNamespaces("Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Geometry");
        }

        boxGeometrySnapshot.ShouldContainPropertyInType("BoxGeometrySnapshot", "SourceNodeId", "int?", "public");
        boxGeometrySnapshot.ShouldContainPropertyInType("BoxGeometrySnapshot", "SourceContentId", "int?", "public");
        boxGeometrySnapshot.ShouldContainPropertyInType("BoxGeometrySnapshot", "SourcePath", "string?", "public");
        boxGeometrySnapshot.ShouldContainPropertyInType("BoxGeometrySnapshot", "SourceOrder", "int?", "public");
        boxGeometrySnapshot.ShouldContainPropertyInType("BoxGeometrySnapshot", "SourceElementIdentity", "string?", "public");
        boxGeometrySnapshot.ShouldContainPropertyInType("BoxGeometrySnapshot", "GeneratedSourceKind", "string?", "public");
    }

    [Fact]
    public void SupportedHtmlVocabulary_HasSingleStyleContractOwner()
    {
        var styleTraversal = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Style", "Style", "StyleTraversal.cs");
        var constants = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Style", "HtmlCssConstants.cs");

        constants.ShouldContainPropertyInType("HtmlCssConstants", "SupportedElementTags", "IReadOnlySet<string>", "public");
        styleTraversal.ShouldUseIdentifier("SupportedElementTags");
        styleTraversal.ShouldNotUseIdentifier("SupportedTags");
    }

    [Fact]
    public void FriendAssemblies_AreExplicitAndLimited()
    {
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Geometry", "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies("Html2x.LayoutEngine", "Html2x.LayoutEngine.Geometry.Test", "Html2x.LayoutEngine.Test");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies("Html2x", "Html2x.LayoutEngine.Geometry.Test", "Html2x.LayoutEngine.Test");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Fragments", "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Fragments.Test",
                "Html2x.LayoutEngine.Geometry.Test",
                "Html2x.LayoutEngine.Test");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                "Html2x",
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Fragments.Test",
                "Html2x.LayoutEngine.Geometry",
                "Html2x.LayoutEngine.Geometry.Test",
                "Html2x.LayoutEngine.Pagination",
                "Html2x.LayoutEngine.Style",
                "Html2x.LayoutEngine.Style.Test",
                "Html2x.LayoutEngine.Test");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Pagination", "Properties", "InternalsVisibleTo.cs")
            .ShouldContainFriendAssemblies(
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Geometry.Test",
                "Html2x.LayoutEngine.Pagination.Test",
                "Html2x.LayoutEngine.Test");
    }
}
