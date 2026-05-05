using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Box.Publishing;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Geometry.Text;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Style.Document;
using Html2x.LayoutEngine.Style.Style;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.Text;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryArchitectureTests
{
    [Fact]
    public void ContractsNamespaces_MatchFolderOwnership()
    {
        CSharpSourceSet.FromDirectory("src", AssemblyName<ComputedStyle>(), "Style")
            .ShouldDeclareNamespace(NamespaceOf<ComputedStyle>());
        CSharpSourceSet.FromDirectory("src", AssemblyName<LayoutGeometryRequest>(), "Geometry")
            .ShouldNotDeclareNamespaces(
                AssemblyName<LayoutBuilder>() + ".Models",
                AssemblyName<LayoutGeometryBuilder>() + ".Published",
                AssemblyName<LayoutGeometryBuilder>() + ".Images",
                NamespaceOf<LayoutGeometryBuilder>());
        CSharpSourceSet.FromDirectory("src", AssemblyName<IImageMetadataResolver>(), "Geometry", "Images")
            .ShouldDeclareNamespace(NamespaceOf<IImageMetadataResolver>());
        CSharpSourceSet.FromDirectory("src", AssemblyName<PublishedLayoutTree>(), "Published")
            .ShouldDeclareNamespace(NamespaceOf<PublishedLayoutTree>());
        CSharpSourceSet.FromDirectory("src", AssemblyName<BlockBox>(), "Models")
            .ShouldDeclareNamespace(NamespaceOf<BlockBox>());
    }

    [Fact]
    public void SharedContractFacts_HaveExpectedOwners()
    {
        var request = SourceFileFor<LayoutGeometryRequest>("Geometry");
        request.ShouldDeclareNamespace(NamespaceOf<LayoutGeometryRequest>());
        request.ShouldContainPropertyInType(
            nameof(LayoutGeometryRequest),
            nameof(LayoutGeometryRequest.ImageMetadataResolver),
            NullableTypeName<IImageMetadataResolver>());
        request.ShouldUseNamespace(NamespaceOf<IImageMetadataResolver>());

        var pageContentArea = SourceFileFor<PageContentArea>("Geometry");
        pageContentArea.ShouldDeclareNamespace(NamespaceOf<PageContentArea>());
        pageContentArea.ShouldContainRecordStruct(nameof(PageContentArea), InternalAccessibility);

        var geometryGuard = SourceFileFor(typeof(GeometryGuard), "Primitives");
        geometryGuard.ShouldDeclareNamespace(NamespaceOf(typeof(GeometryGuard)));
        geometryGuard.ShouldContainType(nameof(GeometryGuard), InternalAccessibility);

        var tablePlacementApplier = SourceFileFor<TablePlacementApplier>("Box");
        tablePlacementApplier.ShouldUseIdentifier(nameof(GeometryTranslator));
        tablePlacementApplier.ShouldNotInvokeMemberOn(nameof(UsedGeometry), nameof(UsedGeometry.Translate));
    }

    [Fact]
    public void ParserDependency_IsOwnedByStyleOnly()
    {
        SourceSetFor<LayoutBuilder>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<LayoutGeometryBuilder>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<LayoutPaginator>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<FragmentBuilder>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<LayoutGeometryRequest>()
            .ShouldNotUseNamespaces(ParserPackageName());
        CSharpSourceSet.FromDirectory("src", "Tests", TestAssemblyNameFor<LayoutGeometryBuilder>())
            .ShouldNotUseNamespaces(ParserPackageName());
    }

    [Fact]
    public void ParserDom_DoesNotLeakIntoHandoffContracts()
    {
        foreach (var file in new[]
        {
            SourceFileFor<StyleTree>("Style"),
            SourceFileFor<StyleNode>("Style"),
            SourceFileFor<StyleContentNode>("Style"),
            SourceFileFor<StyledElementFacts>("Style"),
            SourceFileFor<BoxNode>("Models")
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
        var styleNode = SourceFileFor<StyleNode>("Style");

        styleNode.ShouldContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Children), ReadOnlyListTypeName<StyleNode>(), PublicAccessibility);
        styleNode.ShouldContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Content), ReadOnlyListTypeName<StyleContentNode>(), PublicAccessibility);
        styleNode.ShouldNotContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Children), ListTypeName<StyleNode>(), PublicAccessibility);
        styleNode.ShouldNotContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Content), ListTypeName<StyleContentNode>(), PublicAccessibility);
        var styleSource = SourceSetFor<StyleTreeBuilder>();
        styleSource.ShouldNotInvokeMemberOn(nameof(StyleNode.Children), nameof(List<int>.Add));
        styleSource.ShouldNotInvokeMemberOn(nameof(StyleNode.Content), nameof(List<int>.Add));
    }

    [Fact]
    public void PaginationModule_UsesRenderFactsAndAuditOnly()
    {
        SemanticProjectFor<LayoutPaginator>()
            .ShouldNotReferenceNamespaces(
                NamespaceOf<LayoutGeometryBuilder>(),
                NamespaceOf<FragmentBuilder>(),
                NamespaceOf<StyleTreeBuilder>(),
                "Html2x.Renderers",
                AssemblyName<ITextMeasurer>(),
                ParserPackageName(),
                SkiaSharpPackageName);

        var fragmentPlacementCloner = SourceFileFor<FragmentPlacementCloner>();
        fragmentPlacementCloner.ShouldUseIdentifier(nameof(UsedGeometry.Translate));
        fragmentPlacementCloner.ShouldNotUseIdentifier("RenderGeometryTranslator");
        fragmentPlacementCloner.ShouldNotUseIdentifier(nameof(GeometryTranslator));

        var paginationOptions = SourceFileFor<PaginationOptions>();
        var paginationResult = SourceFileFor<PaginationResult>();
        var paginationDecisionKind = SourceFileFor<PaginationDecisionKind>();
        var paginationPlacementAudit = SourceFileFor<PaginationPlacementAudit>();

        paginationOptions.ShouldContainType(nameof(PaginationOptions), InternalAccessibility, isSealed: true);
        paginationResult.ShouldContainType(nameof(PaginationResult), InternalAccessibility, isSealed: true);
        paginationResult.ShouldContainPropertyInType(nameof(PaginationResult), nameof(PaginationResult.Layout), TypeName<HtmlLayout>(), PublicAccessibility);
        paginationResult.ShouldContainPropertyInType(
            nameof(PaginationResult),
            nameof(PaginationResult.AuditPages),
            ReadOnlyListTypeName<PaginationPageAudit>(),
            PublicAccessibility);
        paginationDecisionKind.ShouldContainEnumMembers(
            nameof(PaginationDecisionKind),
            nameof(PaginationDecisionKind.MovedToNextPage),
            nameof(PaginationDecisionKind.SplitAcrossPages),
            nameof(PaginationDecisionKind.ForcedBreak));
        paginationPlacementAudit.ShouldNotContainPropertyInType(
            nameof(PaginationPlacementAudit),
            "Fragment",
            TypeName<BlockFragment>(),
            PublicAccessibility);
    }

    [Fact]
    public void ProductionGeometry_DoesNotUseSystemDrawingPrimitives()
    {
        foreach (var sourceRoot in new[]
        {
            SourceSetFor<HtmlLayout>(),
            SourceSetFor<StyleNode>(),
            SourceSetFor<LayoutGeometryBuilder>(),
            SourceSetFor<FragmentBuilder>(),
            SourceSetFor<LayoutPaginator>(),
            CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
        })
        {
            sourceRoot.ShouldNotUseNamespaces("System.Drawing");
            sourceRoot.ShouldNotUseIdentifiers("RectangleF", "PointF", "SizeF");
        }
    }

    [Fact]
    public void LayoutComposition_StaysAtStageAndHandoffBoundaries()
    {
        SemanticProjectFor<LayoutBuilder>()
            .ShouldNotReferenceNamespaces(ParserPackageName(), "Html2x.Renderers", SkiaSharpPackageName);

        var layoutBuilder = SourceFileFor<LayoutBuilder>();

        layoutBuilder.ShouldContainMethodInType(nameof(LayoutBuilder), nameof(LayoutBuilder.BuildAsync), TaskTypeName<HtmlLayout>(), PublicAccessibility);
        layoutBuilder.ShouldNotConstructType(nameof(AngleSharpDomProvider));
        layoutBuilder.ShouldNotConstructType(nameof(CssStyleComputer));
        layoutBuilder.ShouldNotConstructType("BoxTreeBuilder");
        layoutBuilder.ShouldNotConstructType(nameof(BlockLayoutEngine));
        layoutBuilder.ShouldNotConstructType(nameof(BlockPaginator));
        layoutBuilder.ShouldNotConstructType(nameof(LayoutPage));
        layoutBuilder.ShouldNotUseIdentifier("CreateLayoutPageChildren");
    }

    [Fact]
    public void LayoutComposition_UsesStageFocusedRunner()
    {
        var layoutBuilder = SourceFileFor<LayoutBuilder>();
        var stageRunner = SourceFileFor<LayoutStageRunner>();
        var stageNames = CSharpSourceFile.Load("src", AssemblyName<LayoutBuilder>(), "LayoutStageNames.cs");
        var snapshotDiagnostics = SourceFileFor(typeof(GeometrySnapshotDiagnostics), "Diagnostics");

        layoutBuilder.ShouldUseIdentifier(nameof(LayoutStageRunner));
        layoutBuilder.ShouldUseIdentifier("CreateGeometryRequest");
        layoutBuilder.ShouldUseIdentifier("CreatePaginationOptions");
        layoutBuilder.ShouldUseIdentifier(nameof(GeometrySnapshotDiagnostics));
        layoutBuilder.ShouldNotUseIdentifier("DiagnosticStage");
        layoutBuilder.ShouldNotConstructType(nameof(DiagnosticRecord));

        stageRunner.ShouldUseIdentifier("DiagnosticStage");
        stageRunner.ShouldUseIdentifier(nameof(LayoutStageNames));
        stageRunner.ShouldUseIdentifier(nameof(LayoutGeometryBuilder));
        stageRunner.ShouldUseIdentifier(nameof(FragmentBuilder));
        stageRunner.ShouldUseIdentifier(nameof(LayoutPaginator));

        stageNames.ShouldContainStringLiteral("stage/box-tree");
        stageNames.ShouldContainStringLiteral("stage/fragment-tree");
        stageNames.ShouldContainStringLiteral("stage/pagination");

        snapshotDiagnostics.ShouldConstructType(nameof(DiagnosticRecord));
        snapshotDiagnostics.ShouldUseIdentifier(nameof(GeometrySnapshotMapper));
        snapshotDiagnostics.ShouldUseIdentifier(nameof(LayoutStageNames));
    }

    [Fact]
    public void FragmentProjection_ConsumesPublishedFactsOnly()
    {
        SemanticProjectFor<FragmentBuilder>()
            .ShouldNotReferenceNamespaces(
                NamespaceOf<BlockBox>(),
                NamespaceOf<StyleTreeBuilder>(),
                "Html2x.Renderers",
                AssemblyName<ITextMeasurer>(),
                ParserPackageName(),
                SkiaSharpPackageName);

        SourceSetFor<FragmentBuilder>()
            .ShouldNotUseIdentifiers(
                nameof(BoxNode),
                "BoxTree",
                nameof(BlockBox),
                nameof(InlineBox),
                nameof(TableBox),
                nameof(ImageBox),
                nameof(RuleBox),
                nameof(BlockLayoutEngine),
                nameof(InlineLayoutEngine),
                nameof(TableGridLayout),
                nameof(StyleTreeBoxProjector),
                nameof(IFontSource));

        var builder = SourceFileFor<FragmentBuilder>();
        builder.ShouldDeclareNamespace(NamespaceOf<FragmentBuilder>());
        builder.ShouldContainType(nameof(FragmentBuilder), InternalAccessibility, isSealed: true);
        builder.ShouldContainMethodInType(nameof(FragmentBuilder), nameof(FragmentBuilder.Build), TypeName<FragmentTree>(), InternalAccessibility);

        var tree = SourceFileFor<FragmentTree>();
        tree.ShouldContainType(nameof(FragmentTree), InternalAccessibility, isSealed: true);
    }

    [Fact]
    public void GeometryRedesign_HasExplicitInternalFlowAndOwnership()
    {
        var layoutGeometryBuilder = SourceFileFor<LayoutGeometryBuilder>();
        var blockLayoutEngine = SourceFileFor<BlockLayoutEngine>("Box");
        var blockFlow = SourceFileFor<BlockFlowLayoutExecutor>("Box");
        var standardRule = SourceFileFor<StandardBlockLayoutRule>("Box");
        var imageRule = SourceFileFor<ImageBlockLayoutRule>("Box");
        var ruleRule = SourceFileFor<RuleBlockLayoutRule>("Box");
        var tableRule = SourceFileFor<TableBlockLayoutRule>("Box");
        var imageApplier = SourceFileFor<ImageBlockLayoutApplier>("Box");
        var tablePlacement = SourceFileFor<TablePlacementApplier>("Box");
        var tableGrid = SourceFileFor<TableGridLayout>("Box");
        var atomicInlineObjectPlacement = SourceFileFor<AtomicInlineObjectPlacement>("Text");

        layoutGeometryBuilder.ShouldUseIdentifier(nameof(StyleTreeBoxProjector));
        blockLayoutEngine.ShouldUseIdentifier(nameof(BlockLayoutRuleSet));
        blockLayoutEngine.ShouldUseIdentifier(nameof(PublishedLayoutWriter));
        blockLayoutEngine.ShouldUseIdentifier(nameof(LayoutBoxStateWriter));
        blockLayoutEngine.ShouldUseIdentifier(nameof(BoxSizingRules));
        blockLayoutEngine.ShouldUseIdentifier(nameof(TableGridLayout));
        blockFlow.ShouldNotUseIdentifiers(
            nameof(StandardBlockLayoutRule),
            nameof(ImageBlockLayoutRule),
            nameof(RuleBlockLayoutRule),
            nameof(TableBlockLayoutRule));
        foreach (var rule in new[] { standardRule, imageRule, ruleRule, tableRule })
        {
            rule.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
            rule.ShouldNotUseIdentifier(nameof(PublishedBlockMapper));
        }

        standardRule.ShouldNotAssignToMember(nameof(BlockBox.TextAlign));
        imageApplier.ShouldNotInvoke(nameof(ImageBox.ApplyImageMetadata));
        imageApplier.ShouldNotInvoke(nameof(BlockBox.ApplyLayoutGeometry));
        tablePlacement.ShouldNotAssignToMember(nameof(BlockBox.Margin));
        tablePlacement.ShouldNotAssignToMember(nameof(BlockBox.Padding));
        tablePlacement.ShouldNotAssignToMember(nameof(BlockBox.TextAlign));
        tablePlacement.ShouldNotInvoke(nameof(BlockBox.ApplyLayoutGeometry));
        tableGrid.ShouldNotUseIdentifier(nameof(LayoutBoxStateWriter));
        atomicInlineObjectPlacement.ShouldUseIdentifier(nameof(LayoutBoxStateWriter));
    }

    [Fact]
    public void RendererSource_ConsumesRenderModelAndResourcesOnly()
    {
        ArchitectureSemanticProject.Load("src", PdfRendererAssemblyName, PdfRendererAssemblyName + ".csproj")
            .ShouldNotReferenceNamespaces(
                AssemblyName<LayoutBuilder>(),
                AssemblyName<StyleNode>(),
                AssemblyName<FragmentBuilder>(),
                NamespaceOf<LayoutGeometryBuilder>(),
                NamespaceOf<StyleTreeBuilder>());

        CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
            .ShouldNotUseIdentifiers(
                nameof(StyleTree),
                nameof(StyleNode),
                nameof(ComputedStyle),
                nameof(PublishedLayoutTree),
                nameof(FragmentBuilder),
                nameof(IFontSource),
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
        var styleSettings = SourceFileFor<StyleBuildSettings>();
        styleSettings.ShouldContainType(nameof(StyleBuildSettings), InternalAccessibility, isSealed: true);
        styleSettings.ShouldContainPropertyInType(
            nameof(StyleBuildSettings),
            nameof(StyleBuildSettings.UseDefaultUserAgentStyleSheet),
            CSharpTypeName<bool>(),
            PublicAccessibility);
        styleSettings.ShouldContainPropertyInType(
            nameof(StyleBuildSettings),
            nameof(StyleBuildSettings.UserAgentStyleSheet),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);

        var layoutSettings = SourceFileFor<LayoutBuildSettings>();
        layoutSettings.ShouldContainType(nameof(LayoutBuildSettings), InternalAccessibility, isSealed: true);
        layoutSettings.ShouldContainPropertyInType(
            nameof(LayoutBuildSettings),
            nameof(LayoutBuildSettings.Style),
            TypeName<StyleBuildSettings>(),
            PublicAccessibility);
        layoutSettings.ShouldContainPropertyInType(
            nameof(LayoutBuildSettings),
            nameof(LayoutBuildSettings.MaxImageSizeBytes),
            CSharpTypeName<long>(),
            PublicAccessibility);

        var pdfSettings = CSharpSourceFile.Load("src", PdfRendererAssemblyName, "PdfRenderSettings.cs");
        pdfSettings.ShouldContainType("PdfRenderSettings", "public", isSealed: true);
        pdfSettings.ShouldContainPropertyInType("PdfRenderSettings", "ResourceBaseDirectory", "string?", "public");
        pdfSettings.ShouldContainPropertyInType("PdfRenderSettings", "MaxImageSizeBytes", "long", "public");

        SemanticProjectFor<LayoutBuilder>()
            .ShouldNotReferenceNamespaces(FacadeAssemblyName + ".Options");
        SemanticProjectFor<StyleTreeBuilder>()
            .ShouldNotReferenceNamespaces(FacadeAssemblyName + ".Options");
        ArchitectureSemanticProject.Load("src", PdfRendererAssemblyName, PdfRendererAssemblyName + ".csproj")
            .ShouldNotReferenceTypes(FacadeAssemblyName + ".HtmlConverterOptions");
    }

    [Fact]
    public void FacadePublicOptions_HaveSingleOwnersForSharedConversionFacts()
    {
        var options = new[]
        {
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "HtmlConverterOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "PageOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "ResourceOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "CssOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "FontOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "DiagnosticsOptions.cs")
        };
        var htmlConverterOptions = options[0];
        var pageOptions = options[1];
        var resourceOptions = options[2];
        var fontOptions = options[4];
        var diagnosticsOptions = options[5];

        foreach (var option in options)
        {
            option.ShouldDeclareNamespace(FacadeAssemblyName + ".Options");
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
        var resourceLoader = CSharpSourceFile.Load("src", ResourcesAssemblyName, "ImageResourceLoader.cs");
        var resourceResult = CSharpSourceFile.Load("src", ResourcesAssemblyName, "ImageResourceResult.cs");
        var resourceMetadataResult = CSharpSourceFile.Load("src", ResourcesAssemblyName, "ImageResourceMetadataResult.cs");
        var metadataResult = SourceFileFor<ImageMetadataResult>("Geometry", "Images");
        var publishedImageFacts = SourceFileFor<PublishedImageFacts>("Published");
        var imageFragment = SourceFileFor<ImageFragment>("Fragments");
        var imageProvider = CSharpSourceFile.Load("src", FacadeAssemblyName, "ImageResourceMetadataResolver.cs");
        var imageRenderer = CSharpSourceFile.Load("src", PdfRendererAssemblyName, "ImageRenderer.cs");

        resourceLoader.ShouldContainType("ImageResourceLoader", "internal");
        resourceLoader.ShouldContainMethodInType("ImageResourceLoader", "Load", "ImageResourceResult", "public");
        resourceLoader.ShouldContainMethodInType("ImageResourceLoader", "ResolveBaseDirectory", "string", "public");
        foreach (var file in new[]
        {
            resourceResult,
            resourceMetadataResult,
            metadataResult,
            publishedImageFacts,
            imageFragment,
            imageRenderer
        })
        {
            file.ShouldUseIdentifier(nameof(ImageLoadStatus));
        }

        foreach (var sourceSet in new[]
        {
            CSharpSourceSet.FromDirectory("src", ResourcesAssemblyName),
            SourceSetFor<IImageMetadataResolver>(),
            SourceSetFor<LayoutGeometryBuilder>(),
            SourceSetFor<FragmentBuilder>(),
            CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
        })
        {
            sourceSet.ShouldNotUseIdentifiers(
                "ImageResourceStatus",
                "ImageMetadataStatus",
                "ImageRenderStatus");
        }

        imageProvider.ShouldUseIdentifier("ImageResourceLoader");
        imageRenderer.ShouldUseIdentifier("ImageResourceLoader");
        imageProvider.ShouldNotUseIdentifier("ToMetadataStatus");
        imageRenderer.ShouldNotUseIdentifier("ToRenderStatus");
        imageProvider.ShouldNotUseIdentifier("DecodeDataUri");
        imageRenderer.ShouldNotUseIdentifier("DecodeDataUri");
    }

    [Fact]
    public void RuntimeOptions_DoNotUseCurrentDirectoryDefaults()
    {
        foreach (var file in new[]
        {
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "HtmlConverterOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "PageOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "ResourceOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "CssOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "FontOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "Options", "DiagnosticsOptions.cs"),
            CSharpSourceFile.Load("src", FacadeAssemblyName, "HtmlConverter.cs"),
            SourceFileFor<LayoutBuildSettings>(),
            SourceFileFor<LayoutGeometryRequest>("Geometry"),
            SourceFileFor<ImageLayoutResolver>("Box"),
            CSharpSourceFile.Load("src", PdfRendererAssemblyName, "PdfRenderSettings.cs"),
            CSharpSourceFile.Load("src", PdfRendererAssemblyName, "ImageRenderer.cs"),
            CSharpSourceFile.Load("src", ResourcesAssemblyName, "ImageResourceLoader.cs")
        })
        {
            file.ShouldNotUseIdentifier("GetCurrentDirectory");
        }
    }

    [Fact]
    public void StageLifecycleDiagnostics_UseCentralEmitter()
    {
        var emitter = SourceFileFor(typeof(DiagnosticStageEmitter));
        var stage = CSharpSourceFile.Load("src", AssemblyName<IDiagnosticsSink>(), "DiagnosticStage.cs");
        var converter = CSharpSourceFile.Load("src", FacadeAssemblyName, "HtmlConverter.cs");
        var layoutBuilder = SourceFileFor<LayoutBuilder>();
        var styleTreeBuilder = SourceFileFor<StyleTreeBuilder>();

        emitter.ShouldContainStringLiteral("stage/started");
        emitter.ShouldContainStringLiteral("stage/succeeded");
        emitter.ShouldContainStringLiteral("stage/failed");
        emitter.ShouldContainStringLiteral("stage/skipped");
        emitter.ShouldContainStringLiteral("stage/cancelled");
        stage.ShouldUseIdentifier(nameof(DiagnosticStageEmitter));
        converter.ShouldUseIdentifier(nameof(DiagnosticStageEmitter));
        layoutBuilder.ShouldNotUseIdentifier("DiagnosticStage");
        SourceFileFor<LayoutStageRunner>().ShouldUseIdentifier("DiagnosticStage");
        styleTreeBuilder.ShouldUseIdentifier("DiagnosticStage");
        foreach (var sourceRoot in new[]
        {
            CSharpSourceSet.FromDirectory("src", FacadeAssemblyName),
            SourceSetFor<LayoutBuilder>(),
            SourceSetFor<StyleTreeBuilder>(),
            SourceSetFor<LayoutGeometryBuilder>(),
            SourceSetFor<LayoutPaginator>(),
            CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
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
