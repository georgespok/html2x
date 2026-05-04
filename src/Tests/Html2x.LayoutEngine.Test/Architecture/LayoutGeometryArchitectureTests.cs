using Html2x.RenderModel;
using Shouldly;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryArchitectureTests
{
    [Fact]
    public void ContractsNamespaces_MatchFolderOwnership()
    {
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Contracts", "Style")
            .ShouldDeclareNamespace("Html2x.LayoutEngine.Contracts.Style");
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Contracts", "Geometry")
            .ShouldNotDeclareNamespaces(
                "Html2x.LayoutEngine.Models",
                "Html2x.LayoutEngine.Geometry.Published",
                "Html2x.LayoutEngine.Geometry.Images",
                "Html2x.LayoutEngine.Geometry");
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Contracts", "Geometry", "Images")
            .ShouldDeclareNamespace("Html2x.LayoutEngine.Contracts.Geometry.Images");
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Contracts", "Published")
            .ShouldDeclareNamespace("Html2x.LayoutEngine.Contracts.Published");
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Geometry", "Models")
            .ShouldDeclareNamespace("Html2x.LayoutEngine.Geometry.Models");
    }

    [Fact]
    public void SharedContractFacts_HaveExpectedOwners()
    {
        var request = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Geometry", "LayoutGeometryRequest.cs");
        request.ShouldDeclareNamespace("Html2x.LayoutEngine.Contracts.Geometry");
        request.ShouldContainPropertyInType("LayoutGeometryRequest", "ImageMetadataResolver", "IImageMetadataResolver?");
        request.ShouldUseNamespace("Html2x.LayoutEngine.Contracts.Geometry.Images");

        var pageContentArea = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Geometry", "PageContentArea.cs");
        pageContentArea.ShouldDeclareNamespace("Html2x.LayoutEngine.Contracts.Geometry");
        pageContentArea.ShouldContainRecordStruct("PageContentArea", "internal");

        var geometryGuard = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Geometry", "Geometry", "GeometryGuard.cs");
        geometryGuard.ShouldDeclareNamespace("Html2x.LayoutEngine.Geometry");
        geometryGuard.ShouldContainType("GeometryGuard", "internal");

        var tablePlacementApplier = CSharpSourceFile.Load(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Box",
            "TablePlacementApplier.cs");
        tablePlacementApplier.ShouldUseIdentifier("GeometryTranslator");
        tablePlacementApplier.ShouldNotInvokeMemberOn("UsedGeometry", "Translate");
    }

    [Fact]
    public void ParserDependency_IsOwnedByStyleOnly()
    {
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine")
            .ShouldNotUseNamespaces(ParserPackageName());
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Geometry")
            .ShouldNotUseNamespaces(ParserPackageName());
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Pagination")
            .ShouldNotUseNamespaces(ParserPackageName());
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Fragments")
            .ShouldNotUseNamespaces(ParserPackageName());
        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Contracts")
            .ShouldNotUseNamespaces(ParserPackageName());
        CSharpSourceSet.FromDirectory("src", "Tests", "Html2x.LayoutEngine.Geometry.Test")
            .ShouldNotUseNamespaces(ParserPackageName());
    }

    [Fact]
    public void ParserDom_DoesNotLeakIntoHandoffContracts()
    {
        foreach (var file in new[]
        {
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleTree.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleNode.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleContentNode.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Style", "StyledElementFacts.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Geometry", "Models", "BoxNode.cs")
        })
        {
            file.ShouldNotUseNamespaces(ParserPackageName());
            file.ShouldNotUseIdentifier("IElement");
            file.ShouldNotUseIdentifier("INode");
            file.ShouldNotUseIdentifier("IDocument");
        }
    }

    [Fact]
    public void StyleNode_HandoffCollections_AreReadOnly()
    {
        var styleNode = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleNode.cs");

        styleNode.ShouldContainPropertyInType("StyleNode", "Children", "IReadOnlyList<StyleNode>", "public");
        styleNode.ShouldContainPropertyInType("StyleNode", "Content", "IReadOnlyList<StyleContentNode>", "public");
        styleNode.ShouldNotContainPropertyInType("StyleNode", "Children", "List<StyleNode>", "public");
        styleNode.ShouldNotContainPropertyInType("StyleNode", "Content", "List<StyleContentNode>", "public");
        var styleSource = CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Style");
        styleSource.ShouldNotInvokeMemberOn("Children", "Add");
        styleSource.ShouldNotInvokeMemberOn("Content", "Add");
    }

    [Fact]
    public void PaginationModule_UsesRenderFactsAndAuditOnly()
    {
        ArchitectureSemanticProject.Load("src", "Html2x.LayoutEngine.Pagination", "Html2x.LayoutEngine.Pagination.csproj")
            .ShouldNotReferenceNamespaces(
                "Html2x.Abstractions",
                "Html2x.LayoutEngine.Geometry",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Style",
                "Html2x.Renderers",
                "Html2x.Text",
                ParserPackageName(),
                "SkiaSharp");

        var fragmentPlacementCloner = CSharpSourceFile.Load(
            "src",
            "Html2x.LayoutEngine.Pagination",
            "FragmentPlacementCloner.cs");
        fragmentPlacementCloner.ShouldUseIdentifier("Translate");
        fragmentPlacementCloner.ShouldNotUseIdentifier("RenderGeometryTranslator");
        fragmentPlacementCloner.ShouldNotUseIdentifier("GeometryTranslator");

        var paginationOptions = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Pagination", "PaginationOptions.cs");
        var paginationResult = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Pagination", "PaginationResult.cs");
        var paginationDecisionKind = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Pagination", "PaginationDecisionKind.cs");
        var paginationPlacementAudit = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Pagination", "PaginationPlacementAudit.cs");

        paginationOptions.ShouldContainType("PaginationOptions", "internal", isSealed: true);
        paginationResult.ShouldContainType("PaginationResult", "internal", isSealed: true);
        paginationResult.ShouldContainPropertyInType("PaginationResult", "Layout", "HtmlLayout", "public");
        paginationResult.ShouldContainPropertyInType("PaginationResult", "AuditPages", "IReadOnlyList<PaginationPageAudit>", "public");
        paginationDecisionKind.ShouldContainEnumMembers("PaginationDecisionKind", "MovedToNextPage", "SplitAcrossPages", "ForcedBreak");
        paginationPlacementAudit.ShouldNotContainPropertyInType("PaginationPlacementAudit", "Fragment", "BlockFragment", "public");
    }

    [Fact]
    public void ProductionGeometry_DoesNotUseSystemDrawingPrimitives()
    {
        foreach (var sourceRoot in new[]
        {
            CSharpSourceSet.FromDirectory("src", "Html2x.RenderModel"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Contracts"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Geometry"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Fragments"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Pagination"),
            CSharpSourceSet.FromDirectory("src", "Html2x.Renderers.Pdf")
        })
        {
            sourceRoot.ShouldNotUseNamespaces("System.Drawing");
            sourceRoot.ShouldNotUseIdentifiers("RectangleF", "PointF", "SizeF");
        }
    }

    [Fact]
    public void LayoutComposition_StaysAtStageAndHandoffBoundaries()
    {
        ArchitectureSemanticProject.Load("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj")
            .ShouldNotReferenceNamespaces(ParserPackageName(), "Html2x.Renderers", "SkiaSharp");

        var layoutBuilder = CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");

        layoutBuilder.ShouldContainMethodInType("LayoutBuilder", "BuildAsync", "Task<HtmlLayout>", "public");
        layoutBuilder.ShouldNotConstructType(ParserDomProviderName());
        layoutBuilder.ShouldNotConstructType(StyleComputerTypeName());
        layoutBuilder.ShouldNotConstructType("BoxTreeBuilder");
        layoutBuilder.ShouldNotConstructType("BlockLayoutEngine");
        layoutBuilder.ShouldNotConstructType("BlockPaginator");
        layoutBuilder.ShouldNotConstructType("LayoutPage");
        layoutBuilder.ShouldNotUseIdentifier("CreateLayoutPageChildren");
    }

    [Fact]
    public void FragmentProjection_ConsumesPublishedFactsOnly()
    {
        ArchitectureSemanticProject.Load("src", "Html2x.LayoutEngine.Fragments", "Html2x.LayoutEngine.Fragments.csproj")
            .ShouldNotReferenceNamespaces(
                "Html2x.Abstractions",
                "Html2x.LayoutEngine.Geometry.Models",
                "Html2x.LayoutEngine.Style",
                "Html2x.Renderers",
                "Html2x.Text",
                ParserPackageName(),
                "SkiaSharp");

        CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Fragments")
            .ShouldNotUseIdentifiers(
                "BoxNode",
                "BoxTree",
                "BlockBox",
                "InlineBox",
                "TableBox",
                "ImageBox",
                "RuleBox",
                "BlockLayoutEngine",
                "InlineLayoutEngine",
                "TableLayoutEngine",
                "InitialBoxTreeBuilder",
                "IFontSource");

        var builder = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Fragments", "FragmentBuilder.cs");
        builder.ShouldDeclareNamespace("Html2x.LayoutEngine.Fragments");
        builder.ShouldContainType("FragmentBuilder", "internal", isSealed: true);
        builder.ShouldContainMethodInType("FragmentBuilder", "Build", "FragmentTree", "internal");

        var tree = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Fragments", "FragmentTree.cs");
        tree.ShouldContainType("FragmentTree", "internal", isSealed: true);
    }

    [Fact]
    public void RendererSource_ConsumesRenderModelAndResourcesOnly()
    {
        ArchitectureSemanticProject.Load("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj")
            .ShouldNotReferenceNamespaces(
                "Html2x.Abstractions",
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Contracts",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Geometry",
                "Html2x.LayoutEngine.Style");

        CSharpSourceSet.FromDirectory("src", "Html2x.Renderers.Pdf")
            .ShouldNotUseIdentifiers(
                "StyleTree",
                "StyleNode",
                "ComputedStyle",
                "PublishedLayoutTree",
                "FragmentBuilder",
                "IFontSource",
                "SourceNodeId",
                "SourceContentId",
                "SourcePath",
                "SourceElementIdentity",
                "GeneratedSourceKind",
                "GeometrySourceIdentity",
                "StyleSourceIdentity",
                "StyleContentIdentity");
    }

    [Fact]
    public void StageOwnedSettings_AreTheInternalOptionBoundary()
    {
        var styleSettings = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Style", "StyleBuildSettings.cs");
        styleSettings.ShouldContainType("StyleBuildSettings", "internal", isSealed: true);
        styleSettings.ShouldContainPropertyInType("StyleBuildSettings", "UseDefaultUserAgentStyleSheet", "bool", "public");
        styleSettings.ShouldContainPropertyInType("StyleBuildSettings", "UserAgentStyleSheet", "string?", "public");

        var layoutSettings = CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "LayoutBuildSettings.cs");
        layoutSettings.ShouldContainType("LayoutBuildSettings", "internal", isSealed: true);
        layoutSettings.ShouldContainPropertyInType("LayoutBuildSettings", "Style", "StyleBuildSettings", "public");
        layoutSettings.ShouldContainPropertyInType("LayoutBuildSettings", "MaxImageSizeBytes", "long", "public");

        var pdfSettings = CSharpSourceFile.Load("src", "Html2x.Renderers.Pdf", "PdfRenderSettings.cs");
        pdfSettings.ShouldContainType("PdfRenderSettings", "public", isSealed: true);
        pdfSettings.ShouldContainPropertyInType("PdfRenderSettings", "HtmlDirectory", "string?", "public");
        pdfSettings.ShouldContainPropertyInType("PdfRenderSettings", "MaxImageSizeBytes", "long", "public");

        ArchitectureSemanticProject.Load("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj")
            .ShouldNotReferenceNamespaces("Html2x.Options");
        ArchitectureSemanticProject.Load("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj")
            .ShouldNotReferenceNamespaces("Html2x.Options");
        ArchitectureSemanticProject.Load("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj")
            .ShouldNotReferenceTypes("Html2x.HtmlConverterOptions");
    }

    [Fact]
    public void FacadePublicOptions_HaveSingleOwnersForSharedConversionFacts()
    {
        var options = new[]
        {
            CSharpSourceFile.Load("src", "Html2x", "Options", "HtmlConverterOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "PageOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "ResourceOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "CssOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "FontOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "DiagnosticsOptions.cs")
        };
        var htmlConverterOptions = options[0];
        var pageOptions = options[1];
        var resourceOptions = options[2];
        var fontOptions = options[4];
        var diagnosticsOptions = options[5];

        foreach (var option in options)
        {
            option.ShouldDeclareNamespace("Html2x");
            option.ShouldNotUseIdentifier("LayoutOptions");
            option.ShouldNotUseIdentifier("PdfOptions");
            option.ShouldNotUseIdentifier("PdfLicenseType");
            option.ShouldNotUseIdentifier("LicenseType");
            option.ShouldNotUseIdentifier("EnableDebugging");
            option.ShouldNotUseIdentifier("MaxImageSizeMb");
        }

        htmlConverterOptions.ShouldContainType("HtmlConverterOptions", "public", isSealed: true);
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Page", "PageOptions", "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Resources", "ResourceOptions", "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Css", "CssOptions", "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Fonts", "FontOptions", "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Diagnostics", "DiagnosticsOptions", "public");
        pageOptions.ShouldContainPropertyInType("PageOptions", "Size", "SizePt", "public");
        resourceOptions.ShouldContainPropertyInType("ResourceOptions", "BaseDirectory", "string?", "public");
        resourceOptions.ShouldContainPropertyInType("ResourceOptions", "MaxImageSizeBytes", "long", "public");
        fontOptions.ShouldContainPropertyInType("FontOptions", "FontPath", "string?", "public");
        diagnosticsOptions.ShouldContainPropertyInType("DiagnosticsOptions", "IncludeRawHtml", "bool", "public");
        diagnosticsOptions.ShouldContainPropertyInType("DiagnosticsOptions", "MaxRawHtmlLength", "int", "public");
    }

    [Fact]
    public void ResourceLoadingPolicy_UsesSharedResourceModuleForImages()
    {
        var resourceLoader = CSharpSourceFile.Load("src", "Html2x.Resources", "ImageResourceLoader.cs");
        var imageProvider = CSharpSourceFile.Load("src", "Html2x", "FileImageProvider.cs");
        var imageRenderer = CSharpSourceFile.Load("src", "Html2x.Renderers.Pdf", "ImageRenderer.cs");

        resourceLoader.ShouldContainType("ImageResourceLoader", "internal");
        resourceLoader.ShouldContainMethodInType("ImageResourceLoader", "Load", "ImageResourceResult", "public");
        resourceLoader.ShouldContainMethodInType("ImageResourceLoader", "ResolveBaseDirectory", "string", "public");
        imageProvider.ShouldUseIdentifier("ImageResourceLoader");
        imageRenderer.ShouldUseIdentifier("ImageResourceLoader");
        imageProvider.ShouldNotUseIdentifier("DecodeDataUri");
        imageRenderer.ShouldNotUseIdentifier("DecodeDataUri");
    }

    [Fact]
    public void RuntimeOptions_DoNotUseCurrentDirectoryDefaults()
    {
        foreach (var file in new[]
        {
            CSharpSourceFile.Load("src", "Html2x", "Options", "HtmlConverterOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "PageOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "ResourceOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "CssOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "FontOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "Options", "DiagnosticsOptions.cs"),
            CSharpSourceFile.Load("src", "Html2x", "HtmlConverter.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "LayoutBuildSettings.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Contracts", "Geometry", "LayoutGeometryRequest.cs"),
            CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Geometry", "Box", "ImageLayoutResolver.cs"),
            CSharpSourceFile.Load("src", "Html2x.Renderers.Pdf", "PdfRenderSettings.cs"),
            CSharpSourceFile.Load("src", "Html2x.Renderers.Pdf", "ImageRenderer.cs"),
            CSharpSourceFile.Load("src", "Html2x.Resources", "ImageResourceLoader.cs")
        })
        {
            file.ShouldNotUseIdentifier("GetCurrentDirectory");
        }
    }

    [Fact]
    public void StageLifecycleDiagnostics_UseCentralEmitter()
    {
        var emitter = CSharpSourceFile.Load("src", "Html2x.Diagnostics.Contracts", "DiagnosticStageEmitter.cs");
        var stage = CSharpSourceFile.Load("src", "Html2x.Diagnostics.Contracts", "DiagnosticStage.cs");
        var converter = CSharpSourceFile.Load("src", "Html2x", "HtmlConverter.cs");
        var layoutBuilder = CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");
        var styleTreeBuilder = CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Style", "StyleTreeBuilder.cs");

        emitter.ShouldContainStringLiteral("stage/started");
        emitter.ShouldContainStringLiteral("stage/succeeded");
        emitter.ShouldContainStringLiteral("stage/failed");
        emitter.ShouldContainStringLiteral("stage/skipped");
        emitter.ShouldContainStringLiteral("stage/cancelled");
        stage.ShouldUseIdentifier("DiagnosticStageEmitter");
        converter.ShouldUseIdentifier("DiagnosticStageEmitter");
        layoutBuilder.ShouldUseIdentifier("DiagnosticStage");
        styleTreeBuilder.ShouldUseIdentifier("DiagnosticStage");
        foreach (var sourceRoot in new[]
        {
            CSharpSourceSet.FromDirectory("src", "Html2x"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Style"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Geometry"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Pagination"),
            CSharpSourceSet.FromDirectory("src", "Html2x.Renderers.Pdf")
        })
        {
            sourceRoot.ShouldNotContainStringLiterals(
                "stage/started",
                "stage/succeeded",
                "stage/failed",
                "stage/skipped",
                "stage/cancelled");
        }
    }

}
