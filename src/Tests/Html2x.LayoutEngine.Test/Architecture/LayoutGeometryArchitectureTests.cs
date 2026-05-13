using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Construction;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Geometry.Publishing;
using Html2x.LayoutEngine.Geometry.Tables;
using Html2x.LayoutEngine.Geometry.Writing;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Style.Document;
using Html2x.LayoutEngine.Style.Computation;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Resources;
using Html2x.Text;
using Shouldly;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;
using System.Text.RegularExpressions;

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
                AssemblyName<LayoutPipeline>() + ".Models",
                AssemblyName<LayoutGeometryConstruction>() + ".Published",
                AssemblyName<LayoutGeometryConstruction>() + ".Images",
                NamespaceOf<LayoutGeometryConstruction>());
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

        var TablePlacementWriter = SourceFileFor<TablePlacementWriter>("Tables");
        TablePlacementWriter.ShouldUseIdentifier(nameof(GeometryTranslator));
        TablePlacementWriter.ShouldNotInvokeMemberOn(nameof(UsedGeometry), nameof(UsedGeometry.Translate));
    }

    [Fact]
    public void ParserDependency_IsOwnedByStyleOnly()
    {
        SourceSetFor<LayoutPipeline>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<LayoutGeometryConstruction>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<LayoutPaginator>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<FragmentTreeBuilder>()
            .ShouldNotUseNamespaces(ParserPackageName());
        SourceSetFor<LayoutGeometryRequest>()
            .ShouldNotUseNamespaces(ParserPackageName());
        CSharpSourceSet.FromDirectory("src", "Tests", TestAssemblyNameFor<LayoutGeometryConstruction>())
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

        styleNode.ShouldContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Children),
            ReadOnlyListTypeName<StyleNode>(), PublicAccessibility);
        styleNode.ShouldContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Content),
            ReadOnlyListTypeName<StyleContentNode>(), PublicAccessibility);
        styleNode.ShouldNotContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Children),
            ListTypeName<StyleNode>(), PublicAccessibility);
        styleNode.ShouldNotContainPropertyInType(nameof(StyleNode), nameof(StyleNode.Content),
            ListTypeName<StyleContentNode>(), PublicAccessibility);
        var styleSource = SourceSetFor<StyleTreeBuilder>();
        styleSource.ShouldNotInvokeMemberOn(nameof(StyleNode.Children), nameof(List<int>.Add));
        styleSource.ShouldNotInvokeMemberOn(nameof(StyleNode.Content), nameof(List<int>.Add));
    }

    [Fact]
    public void PaginationModule_UsesRenderFactsAndAuditOnly()
    {
        SemanticProjectFor<LayoutPaginator>()
            .ShouldNotReferenceNamespaces(
                NamespaceOf<LayoutGeometryConstruction>(),
                NamespaceOf<FragmentTreeBuilder>(),
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

        paginationOptions.ShouldContainType(nameof(PaginationOptions), InternalAccessibility, true);
        paginationResult.ShouldContainType(nameof(PaginationResult), InternalAccessibility, true);
        paginationResult.ShouldContainPropertyInType(nameof(PaginationResult), nameof(PaginationResult.Layout),
            TypeName<HtmlLayout>(), PublicAccessibility);
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
                     SourceSetFor<LayoutGeometryConstruction>(),
                     SourceSetFor<FragmentTreeBuilder>(),
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
        SemanticProjectFor<LayoutPipeline>()
            .ShouldNotReferenceNamespaces(ParserPackageName(), "Html2x.Renderers", SkiaSharpPackageName);

        var layoutPipeline = SourceFileFor<LayoutPipeline>();

        layoutPipeline.ShouldContainMethodInType(nameof(LayoutPipeline), nameof(LayoutPipeline.BuildAsync),
            TaskTypeName<HtmlLayout>(), PublicAccessibility);
        layoutPipeline.ShouldNotConstructType(nameof(AngleSharpDocumentLoader));
        layoutPipeline.ShouldNotConstructType(nameof(CssStyleComputer));
        layoutPipeline.ShouldNotConstructType("BoxTreeBuilder");
        layoutPipeline.ShouldNotConstructType(nameof(BlockBoxLayout));
        layoutPipeline.ShouldNotConstructType(nameof(BlockFormattingMetricsMeasurement));
        layoutPipeline.ShouldNotConstructType(nameof(BlockPaginator));
        layoutPipeline.ShouldNotConstructType(nameof(LayoutPage));
        layoutPipeline.ShouldNotUseIdentifier("CreateLayoutPageChildren");
    }

    [Fact]
    public void LayoutComposition_UsesStageFocusedRunner()
    {
        var layoutPipeline = SourceFileFor<LayoutPipeline>();
        var stageRunner = SourceFileFor<LayoutStageRunner>();
        var stageNames = CSharpSourceFile.Load("src", AssemblyName<LayoutPipeline>(), "LayoutStageNames.cs");
        var snapshotDiagnostics = SourceFileFor(typeof(GeometrySnapshotDiagnostics), "Diagnostics");

        layoutPipeline.ShouldUseIdentifier(nameof(LayoutStageRunner));
        layoutPipeline.ShouldUseIdentifier("CreateGeometryRequest");
        layoutPipeline.ShouldUseIdentifier("CreatePaginationOptions");
        layoutPipeline.ShouldUseIdentifier(nameof(GeometrySnapshotDiagnostics));
        layoutPipeline.ShouldNotUseIdentifier("DiagnosticStageRunner");
        layoutPipeline.ShouldNotConstructType(nameof(DiagnosticRecord));

        stageRunner.ShouldUseIdentifier("DiagnosticStageRunner");
        stageRunner.ShouldUseIdentifier(nameof(LayoutStageNames));
        stageRunner.ShouldUseIdentifier(nameof(LayoutGeometryConstruction));
        stageRunner.ShouldUseIdentifier(nameof(FragmentTreeBuilder));
        stageRunner.ShouldUseIdentifier(nameof(LayoutPaginator));

        stageNames.ShouldContainStringLiteral("stage/box-tree");
        stageNames.ShouldContainStringLiteral("stage/fragment-tree");
        stageNames.ShouldContainStringLiteral("stage/pagination");

        snapshotDiagnostics.ShouldConstructType(nameof(DiagnosticRecord));
        snapshotDiagnostics.ShouldUseIdentifier(nameof(GeometrySnapshotMapper));
        snapshotDiagnostics.ShouldUseIdentifier(nameof(LayoutStageNames));
    }

    [Fact]
    public void FragmentTreeBuilding_ConsumesPublishedFactsOnly()
    {
        SemanticProjectFor<FragmentTreeBuilder>()
            .ShouldNotReferenceNamespaces(
                NamespaceOf<BlockBox>(),
                NamespaceOf<StyleTreeBuilder>(),
                "Html2x.Renderers",
                AssemblyName<ITextMeasurer>(),
                ParserPackageName(),
                SkiaSharpPackageName);

        SourceSetFor<FragmentTreeBuilder>()
            .ShouldNotUseIdentifiers(
                nameof(BoxNode),
                "BoxTree",
                nameof(BlockBox),
                nameof(InlineBox),
                nameof(TableBox),
                nameof(ImageBox),
                nameof(RuleBox),
                nameof(BlockBoxLayout),
                nameof(InlineFlowLayout),
                nameof(TableGridLayout),
                nameof(BoxTreeConstruction),
                nameof(IFontSource));

        var builder = SourceFileFor<FragmentTreeBuilder>();
        builder.ShouldDeclareNamespace(NamespaceOf<FragmentTreeBuilder>());
        builder.ShouldContainType(nameof(FragmentTreeBuilder), InternalAccessibility, true);
        builder.ShouldContainMethodInType(nameof(FragmentTreeBuilder), nameof(FragmentTreeBuilder.Build),
            TypeName<FragmentTree>(), InternalAccessibility);

        var tree = SourceFileFor<FragmentTree>();
        tree.ShouldContainType(nameof(FragmentTree), InternalAccessibility, true);
    }

    [Fact]
    public void GeometryRedesign_HasExplicitInternalFlowAndOwnership()
    {
        var layoutGeometryBuilder = SourceFileFor<LayoutGeometryConstruction>();
        var geometryPipelineComposer = CSharpSourceFile.Load(
            "src",
            AssemblyName<LayoutGeometryConstruction>(),
            "Composition",
            "GeometryPipelineConstruction.cs");
        var boxTreeLayout = SourceFileFor<BoxTreeLayout>("BlockFlow");
        var blockBoxLayout = SourceFileFor<BlockBoxLayout>("BlockFlow");
        var blockFlow = SourceFileFor<BlockFlowLayout>("BlockFlow");
        var standardRule = SourceFileFor<StandardBlockLayoutRule>("BlockFlow");
        var imageRule = SourceFileFor<ImageBlockLayoutRule>("Images");
        var ruleRule = SourceFileFor<RuleBlockLayoutRule>("BlockFlow");
        var tableRule = SourceFileFor<TableBlockLayoutRule>("Tables");
        var imageWriter = SourceFileFor<ImageBlockLayoutWriter>("Images");
        var tablePlacement = SourceFileFor<TablePlacementWriter>("Tables");
        var tableGrid = SourceFileFor<TableGridLayout>("Tables");
        var atomicInlineBoxPlacementWriter = SourceFileFor<AtomicInlineBoxPlacementWriter>("InlineFlow");
        var publishedLayoutWriter = SourceFileFor<PublishedLayoutWriter>("Publishing");

        layoutGeometryBuilder.ShouldUseIdentifier(nameof(BoxTreeConstruction));
        geometryPipelineComposer.ShouldConstructType(nameof(BoxTreeLayout));
        boxTreeLayout.ShouldUseIdentifier(nameof(BlockBoxLayout));
        boxTreeLayout.ShouldUseIdentifier(nameof(PageContentArea));
        boxTreeLayout.ShouldUseIdentifier(nameof(PublishedLayoutTree));
        boxTreeLayout.ShouldUseIdentifier(nameof(BlockStackLayoutRequest));
        boxTreeLayout.ShouldNotUseIdentifier(nameof(BlockLayoutRuleSet));
        blockBoxLayout.ShouldNotUseIdentifier(nameof(BoxTreeLayout));
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockLayoutRuleSet));
        blockBoxLayout.ShouldUseIdentifier("CreateDefaultRuleSet");
        blockBoxLayout.ShouldNotContainStringLiteral("Block layout rules were used before initialization.");
        blockBoxLayout.ShouldUseIdentifier(nameof(PublishedLayoutWriter));
        blockBoxLayout.ShouldUseIdentifier(nameof(LayoutBoxStateWriter));
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockSizingRules));
        blockBoxLayout.ShouldUseIdentifier(nameof(TableGridLayout));
        blockBoxLayout.ShouldNotUseIdentifier(nameof(PageContentArea));
        blockBoxLayout.ShouldInvoke(nameof(PublishedLayoutWriter.WriteRuleResult));
        blockBoxLayout.ShouldNotUseIdentifier(nameof(PublishedBlockFacts));
        blockBoxLayout.ShouldNotConstructType(nameof(PublishedChildBlockItem));
        blockBoxLayout.ShouldNotConstructType(nameof(PublishedInlineFlowSegmentItem));
        blockBoxLayout.ShouldNotConstructType(nameof(PublishedInlineObjectItem));
        blockFlow.ShouldNotUseIdentifier(nameof(BlockLayoutRuleSet));
        blockFlow.ShouldNotUseIdentifier(nameof(IBlockLayoutRule));
        blockFlow.ShouldUseIdentifier(nameof(LayoutBoxStateWriter));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedBlock));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedInlineLayout));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedBlockFlowItem));
        blockFlow.ShouldInvoke(nameof(LayoutBoxStateWriter.ApplyInlineLayout));
        blockFlow.ShouldNotAssignToMember(nameof(BlockBox.InlineLayout));
        blockFlow.ShouldNotUseIdentifiers(
            nameof(StandardBlockLayoutRule),
            nameof(ImageBlockLayoutRule),
            nameof(RuleBlockLayoutRule),
            nameof(TableBlockLayoutRule));
        foreach (var rule in new[] { standardRule, imageRule, ruleRule, tableRule })
        {
            rule.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
            rule.ShouldNotUseIdentifier(nameof(PublishedBlockFacts));
            rule.ShouldNotUseIdentifier(nameof(PublishedBlock));
            rule.ShouldNotUseIdentifier(nameof(PublishedInlineLayout));
            rule.ShouldNotUseIdentifier(nameof(PublishedBlockFlowItem));
        }

        publishedLayoutWriter.ShouldContainMethodInType(
            nameof(PublishedLayoutWriter),
            nameof(PublishedLayoutWriter.WriteRuleResult));
        publishedLayoutWriter.ShouldUseIdentifier(nameof(PublishedBlockFacts));
        publishedLayoutWriter.ShouldConstructType(nameof(PublishedChildBlockItem));
        publishedLayoutWriter.ShouldConstructType(nameof(PublishedInlineFlowSegmentItem));
        publishedLayoutWriter.ShouldConstructType(nameof(PublishedInlineObjectItem));
        standardRule.ShouldNotAssignToMember(nameof(BlockBox.TextAlign));
        imageWriter.ShouldNotInvoke(nameof(ImageBox.ApplyImageMetadata));
        imageWriter.ShouldNotInvoke(nameof(BlockBox.ApplyLayoutGeometry));
        tablePlacement.ShouldNotAssignToMember(nameof(BlockBox.Margin));
        tablePlacement.ShouldNotAssignToMember(nameof(BlockBox.Padding));
        tablePlacement.ShouldNotAssignToMember(nameof(BlockBox.TextAlign));
        tablePlacement.ShouldNotInvoke(nameof(BlockBox.ApplyLayoutGeometry));
        tableGrid.ShouldNotUseIdentifier(nameof(LayoutBoxStateWriter));
        atomicInlineBoxPlacementWriter.ShouldUseIdentifier(nameof(LayoutBoxStateWriter));
    }

    [Fact]
    public void GeometryMutableStateWrites_AreRoutedThroughStateWriterOrConstructionBoundaries()
    {
        var geometryRoot = PathFromRoot("src", AssemblyName<LayoutGeometryConstruction>());
        var allowedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "src/Html2x.LayoutEngine.Geometry/Construction/BoxTreeConstruction.cs",
            "src/Html2x.LayoutEngine.Geometry/Construction/BoxTreeNormalization.cs",
            "src/Html2x.LayoutEngine.Geometry/Writing/LayoutBoxStateWriter.cs",
            "src/Html2x.LayoutEngine.Geometry/Models/BlockBox.cs",
            "src/Html2x.LayoutEngine.Geometry/Models/ImageBox.cs"
        };
        var mutationPatterns = new[]
        {
            new Regex(
                @"\.(UsedGeometry|InlineLayout|Margin|Padding|TextAlign|DerivedColumnCount|RowIndex|ColumnIndex|IsHeader)\s*=(?!=)",
                RegexOptions.Compiled),
            new Regex(@"\.(ApplyLayoutGeometry|ApplyImageMetadata)\s*\(", RegexOptions.Compiled)
        };

        var violations = Directory
            .GetFiles(geometryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsGeneratedOrBuildOutput(path))
            .Where(path => !allowedFiles.Contains(RelativeSourcePath(path)))
            .SelectMany(path => File
                .ReadLines(path)
                .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
            .Where(item => mutationPatterns.Any(pattern => pattern.IsMatch(item.Line)))
            .Select(item => $"{RelativeSourcePath(item.Path)}:{item.Number}: {item.Line.Trim()}")
            .ToArray();

        violations.ShouldBeEmpty(
            "Mutable layout state should be assigned only by LayoutBoxStateWriter or documented construction/model copy boundaries. "
            + string.Join(" ", violations));
    }

    [Fact]
    public void GeometryChildStateMutation_UsesExplicitBoxNodeMethods()
    {
        var boxNode = SourceFileFor<BoxNode>("Models");
        boxNode.ShouldContainPropertyInType(
            nameof(BoxNode),
            nameof(BoxNode.Children),
            ReadOnlyListTypeName<BoxNode>(),
            PublicAccessibility);
        boxNode.ShouldNotContainPropertyInType(
            nameof(BoxNode),
            nameof(BoxNode.Children),
            ListTypeName<BoxNode>(),
            PublicAccessibility);
        boxNode.ShouldContainMethodInType(nameof(BoxNode), "AddChild", VoidTypeName, InternalAccessibility);
        boxNode.ShouldContainMethodInType(nameof(BoxNode), "InsertChild", VoidTypeName, InternalAccessibility);
        boxNode.ShouldContainMethodInType(nameof(BoxNode), "ReplaceChildren", VoidTypeName, InternalAccessibility);
        boxNode.ShouldContainMethodInType(nameof(BoxNode), "ClearChildren", VoidTypeName, InternalAccessibility);

        var geometryRoot = PathFromRoot("src", AssemblyName<LayoutGeometryConstruction>());
        var childMutationPatterns = new[]
        {
            new Regex(@"\.Children\.(Add|Insert|Clear|AddRange|Remove|RemoveAt)\s*\(", RegexOptions.Compiled),
            new Regex(@"\.Children\[[^\]]+\]\s*=", RegexOptions.Compiled)
        };

        var violations = Directory
            .GetFiles(geometryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsGeneratedOrBuildOutput(path))
            .SelectMany(path => File
                .ReadLines(path)
                .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
            .Where(item => childMutationPatterns.Any(pattern => pattern.IsMatch(item.Line)))
            .Select(item => $"{RelativeSourcePath(item.Path)}:{item.Number}: {item.Line.Trim()}")
            .ToArray();

        violations.ShouldBeEmpty(
            "Production Layout Geometry should mutate box children through BoxNode child-state methods. "
            + string.Join(" ", violations));
    }

    [Fact]
    public void GeometryMeasurementPaths_DoNotWriteMutableLayoutState()
    {
        var measurementFiles = new[]
        {
            SourceFileFor<BlockContentSizeMeasurement>("Measurement"),
            SourceFileFor<BlockFormattingMetricsMeasurement>("Measurement"),
            SourceFileFor<BlockFlowMeasurement>("Measurement"),
            SourceFileFor<InlineFlowMeasurement>("Measurement"),
            SourceFileFor<BlockSizingRules>("BlockFlow"),
            SourceFileFor<BlockContentSizeFacts>("Measurement"),
            SourceFileFor<InlineContentSizeFacts>("Measurement"),
            SourceFileFor<TableCellMeasurement>("Measurement"),
            SourceFileFor<TableGridLayout>("Tables"),
            SourceFileFor<AtomicInlineBoxLayout>("InlineFlow")
        };

        SourceFileFor<BlockContentSizeMeasurement>("Measurement").ShouldUseIdentifier(nameof(BlockSizingRules));
        SourceFileFor<BlockContentSizeMeasurement>("Measurement").ShouldUseIdentifier(nameof(InlineContentSizeFacts));
        SourceFileFor<InlineFlowMeasurement>("Measurement").ShouldNotUseIdentifier("InlineLayoutResult");
        SourceFileFor<StandardBlockLayoutRule>("BlockFlow").ShouldUseIdentifier(nameof(BlockSizingRules));
        SourceFileFor<TableGridLayout>("Tables").ShouldUseIdentifier(nameof(BlockSizingRules));
        SourceFileFor<AtomicInlineBoxLayout>("InlineFlow").ShouldUseIdentifier(nameof(BlockSizingRules));

        foreach (var file in measurementFiles)
        {
            file.ShouldNotUseIdentifier(nameof(LayoutBoxStateWriter));
            file.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
            file.ShouldNotUseIdentifier(nameof(PublishedBlockFacts));
            file.ShouldNotUseIdentifier(nameof(PublishedLayoutTree));
            file.ShouldNotInvoke(nameof(BlockBox.ApplyLayoutGeometry));
            file.ShouldNotInvoke(nameof(ImageBox.ApplyImageMetadata));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyBlockLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyImageBlockLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyInlineLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyInlineBoxContentLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyTableCellLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyTableLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyTableRowLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyUnsupportedTablePlaceholder));
            file.ShouldNotAssignToMember(nameof(BlockBox.UsedGeometry));
            file.ShouldNotAssignToMember(nameof(BlockBox.InlineLayout));
        }
    }

    [Fact]
    public void PreLayoutMeasurement_DoesNotReadUsedGeometry()
    {
        foreach (var file in new[]
                 {
                     SourceFileFor<BlockContentSizeMeasurement>("Measurement"),
                     SourceFileFor<BlockFormattingMetricsMeasurement>("Measurement"),
                     SourceFileFor<BlockFlowMeasurement>("Measurement"),
                     SourceFileFor<InlineFlowMeasurement>("Measurement"),
                     SourceFileFor<InlineContentSizeFacts>("Measurement"),
                     SourceFileFor<TableCellMeasurement>("Measurement"),
                     SourceFileFor<AtomicInlineBoxLayout>("InlineFlow")
                 })
        {
            file.ShouldNotUseIdentifier(nameof(UsedGeometry));
        }
    }

    [Fact]
    public void ProductionGeometry_UsesPrimitiveAuthoritiesForUsedGeometryTransforms()
    {
        var geometryRoot = PathFromRoot("src", AssemblyName<LayoutGeometryConstruction>());
        var allowedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "src/Html2x.LayoutEngine.Geometry/Primitives/GeometryTranslator.cs",
            "src/Html2x.LayoutEngine.Geometry/Primitives/UsedGeometryRules.cs"
        };
        var helperPatterns = new[]
        {
            new Regex(@"\.Translate\s*\(", RegexOptions.Compiled),
            new Regex(@"\.WithBorderX\s*\(", RegexOptions.Compiled),
            new Regex(@"\.WithBorderY\s*\(", RegexOptions.Compiled),
            new Regex(@"\.WithBorderWidth\s*\(", RegexOptions.Compiled),
            new Regex(@"\.WithBorderHeight\s*\(", RegexOptions.Compiled),
            new Regex(@"\.WithContentInsets\s*\(", RegexOptions.Compiled)
        };

        var violations = Directory
            .GetFiles(geometryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsGeneratedOrBuildOutput(path))
            .Where(path => !allowedFiles.Contains(RelativeSourcePath(path)))
            .SelectMany(path => File
                .ReadLines(path)
                .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
            .Where(static item => !item.Line.Contains("GeometryTranslator.Translate", StringComparison.Ordinal))
            .Where(static item => !item.Line.Contains("UsedGeometryRules.", StringComparison.Ordinal))
            .Where(item => helperPatterns.Any(pattern => pattern.IsMatch(item.Line)))
            .Select(item => $"{RelativeSourcePath(item.Path)}:{item.Number}: {item.Line.Trim()}")
            .ToArray();

        violations.ShouldBeEmpty(
            "Production Layout Geometry should route UsedGeometry transforms through GeometryTranslator or UsedGeometryRules. "
            + string.Join(" ", violations));
    }

    [Fact]
    public void GeometryModuleNames_FollowApprovedGrammarOrDocumentedException()
    {
        var retiredSuffixes = new[]
        {
            "Projector",
            "Factory",
            "Appender",
            "Inserter",
            "Resolver",
            "Engine",
            "Executor",
            "Applier",
            "Classifier",
            "Context",
            "Calculator",
            "Builder",
            "Mapper",
            "Planner",
            "Materializer",
            "Manager",
            "Helper"
        };
        var documentedExceptions = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(LayoutGeometryConstruction)
        };
        var geometryRoot = PathFromRoot("src", AssemblyName<LayoutGeometryConstruction>());

        var violations = Directory
            .GetFiles(geometryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsGeneratedOrBuildOutput(path))
            .Select(static path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !documentedExceptions.Contains(name))
            .Select(name => new
            {
                Name = name,
                RetiredSuffix =
                    retiredSuffixes.FirstOrDefault(suffix => name.EndsWith(suffix, StringComparison.Ordinal))
            })
            .Where(match => match.RetiredSuffix is not null)
            .Select(match => $"{match.Name} uses retired suffix {match.RetiredSuffix}.")
            .ToArray();

        violations.Length.ShouldBe(
            0,
            "Geometry module names should use Construction, Layout, Measurement, Rules, Writer, Request, Result, Facts, or Rule unless explicitly documented. "
            + string.Join(" ", violations));
    }

    [Fact]
    public void RendererSource_ConsumesRenderModelAndResourcesOnly()
    {
        ArchitectureSemanticProject.Load("src", PdfRendererAssemblyName, PdfRendererAssemblyName + ".csproj")
            .ShouldNotReferenceNamespaces(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<StyleNode>(),
                AssemblyName<FragmentTreeBuilder>(),
                NamespaceOf<LayoutGeometryConstruction>(),
                NamespaceOf<StyleTreeBuilder>());

        CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
            .ShouldNotUseIdentifiers(
                nameof(StyleTree),
                nameof(StyleNode),
                nameof(ComputedStyle),
                nameof(PublishedLayoutTree),
                nameof(FragmentTreeBuilder),
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
        styleSettings.ShouldContainType(nameof(StyleBuildSettings), InternalAccessibility, true);
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
        layoutSettings.ShouldContainType(nameof(LayoutBuildSettings), InternalAccessibility, true);
        layoutSettings.ShouldContainPropertyInType(
            nameof(LayoutBuildSettings),
            nameof(LayoutBuildSettings.Style),
            TypeName<StyleBuildSettings>(),
            PublicAccessibility);
        var pdfSettings = CSharpSourceFile.Load("src", PdfRendererAssemblyName, "PdfRenderSettings.cs");
        pdfSettings.ShouldContainType("PdfRenderSettings", InternalAccessibility, true);
        pdfSettings.ShouldContainPropertyInType("PdfRenderSettings", "ResourceBaseDirectory", "string?", "public");
        pdfSettings.ShouldContainPropertyInType("PdfRenderSettings", "MaxImageSizeBytes", "long", "public");

        SemanticProjectFor<LayoutPipeline>()
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

        htmlConverterOptions.ShouldContainType("HtmlConverterOptions", "public", true);
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Page", "PageOptions", "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Resources", "ResourceOptions",
            "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Css", "CssOptions", "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Fonts", "FontOptions", "public");
        htmlConverterOptions.ShouldContainPropertyInType("HtmlConverterOptions", "Diagnostics", "DiagnosticsOptions",
            "public");
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
        var resourceMetadataResult =
            CSharpSourceFile.Load("src", ResourcesAssemblyName, "ImageResourceMetadataResult.cs");
        var resourceStore = CSharpSourceFile.Load("src", ResourcesAssemblyName, "ImageResourceStore.cs");
        var imageLoadStatus = SourceFileFor<ImageLoadStatus>("Resources");
        var metadataResult = SourceFileFor<ImageMetadataResult>("Geometry", "Images");
        var publishedImageFacts = SourceFileFor<PublishedImageFacts>("Published");
        var imageFragment = SourceFileFor<ImageFragment>("Fragments");
        var imageProvider = CSharpSourceFile.Load("src", FacadeAssemblyName, "ImageResourceMetadataResolver.cs");
        var imageRenderer = CSharpSourceFile.Load("src", PdfRendererAssemblyName, "ImageRenderer.cs");

        resourceLoader.ShouldContainType("ImageResourceLoader", "internal");
        resourceLoader.ShouldContainMethodInType("ImageResourceLoader", "Load", "ImageResourceResult", "public");
        resourceLoader.ShouldContainMethodInType("ImageResourceLoader", "ResolveBaseDirectory", "string", "public");
        imageLoadStatus.ShouldDeclareNamespace(NamespaceOf<ImageLoadStatus>());
        imageLoadStatus.ShouldContainEnum(nameof(ImageLoadStatus), PublicAccessibility);
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
                     SourceSetFor<LayoutGeometryConstruction>(),
                     SourceSetFor<FragmentTreeBuilder>(),
                     CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
                 })
        {
            sourceSet.ShouldNotUseIdentifiers(
                "ImageResourceStatus",
                "ImageMetadataStatus",
                "ImageRenderStatus");
        }

        resourceStore.ShouldUseIdentifier("ImageResourceLoader");
        resourceStore.ShouldUseIdentifier("ImageResourceResult");
        resourceStore.ShouldUseIdentifier("ImageResourceMetadataResult");
        imageProvider.ShouldUseIdentifier("ImageResourceStore");
        imageRenderer.ShouldUseIdentifier("IImageResourceReader");
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
                     SourceFileFor<ImageSizingRules>("Images"),
                     CSharpSourceFile.Load("src", ResourcesAssemblyName, "ImageResourceStore.cs"),
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
        var stage = CSharpSourceFile.Load("src", AssemblyName<IDiagnosticsSink>(), "DiagnosticStageRunner.cs");
        var converter = CSharpSourceFile.Load("src", FacadeAssemblyName, "HtmlConverter.cs");
        var layoutPipeline = SourceFileFor<LayoutPipeline>();
        var styleTreeBuilder = SourceFileFor<StyleTreeBuilder>();

        emitter.ShouldContainStringLiteral("stage/started");
        emitter.ShouldContainStringLiteral("stage/succeeded");
        emitter.ShouldContainStringLiteral("stage/failed");
        emitter.ShouldContainStringLiteral("stage/skipped");
        emitter.ShouldContainStringLiteral("stage/canceled");
        stage.ShouldUseIdentifier(nameof(DiagnosticStageEmitter));
        converter.ShouldUseIdentifier(nameof(DiagnosticStageEmitter));
        layoutPipeline.ShouldNotUseIdentifier("DiagnosticStageRunner");
        SourceFileFor<LayoutStageRunner>().ShouldUseIdentifier("DiagnosticStageRunner");
        styleTreeBuilder.ShouldUseIdentifier("DiagnosticStageRunner");
        foreach (var sourceRoot in new[]
                 {
                     CSharpSourceSet.FromDirectory("src", FacadeAssemblyName),
                     SourceSetFor<LayoutPipeline>(),
                     SourceSetFor<StyleTreeBuilder>(),
                     SourceSetFor<LayoutGeometryConstruction>(),
                     SourceSetFor<LayoutPaginator>(),
                     CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
                 })
        {
            sourceRoot.ShouldNotContainStringLiterals(
                "stage/started",
                "stage/succeeded",
                "stage/failed",
                "stage/skipped",
                "stage/canceled");
        }
    }

    private static bool IsGeneratedOrBuildOutput(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
               || segments.Contains("obj", StringComparer.OrdinalIgnoreCase)
               || path.EndsWith(".g.cs", StringComparison.Ordinal)
               || path.EndsWith(".AssemblyInfo.cs", StringComparison.Ordinal);
    }

    private static string RelativeSourcePath(string path) =>
        Path.GetRelativePath(ArchitecturePaths.RepoRoot(), path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
}
