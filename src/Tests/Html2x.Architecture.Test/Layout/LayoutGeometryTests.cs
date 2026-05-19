using System.Drawing;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine;
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
using Html2x.LayoutEngine.Stage.Contracts.Geometry;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Style.Document;
using Html2x.LayoutEngine.Style.Computation;
using Html2x.Options;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Resources;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.Resources;
using Html2x.Text;
using Shouldly;
using Html2x.Architecture.Test.Support;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test.Layout;

public sealed class LayoutGeometryTests
{
    [Fact]
    public void ContractsNamespaces_MatchFolderOwnership()
    {
        SourceSetForNamespaceOf<ComputedStyle>()
            .ShouldDeclareNamespace(NamespaceOf<ComputedStyle>());
        SourceSetForNamespaceOf<LayoutGeometryRequest>()
            .ShouldNotDeclareNamespaces(
                AssemblyName<LayoutPipeline>() + "." + NamespaceSegmentOf<BlockBox>(),
                AssemblyName<LayoutGeometryConstruction>() + "." + NamespaceSegmentOf<PublishedLayoutWriter>(),
                AssemblyName<LayoutGeometryConstruction>() + "." + NamespaceSegmentOf<ImageSizingRules>(),
                NamespaceOf<LayoutGeometryConstruction>());
        SourceSetForNamespaceOf<IImageMetadataResolver>()
            .ShouldDeclareNamespace(NamespaceOf<IImageMetadataResolver>());
        SourceSetForNamespaceOf<PublishedLayoutTree>()
            .ShouldDeclareNamespace(NamespaceOf<PublishedLayoutTree>());
        SourceSetForNamespaceOf<BlockBox>()
            .ShouldDeclareNamespace(NamespaceOf<BlockBox>());
    }

    [Fact]
    public void SharedContractFacts_HaveExpectedOwners()
    {
        var request = SourceFileFor<LayoutGeometryRequest>();
        request.ShouldDeclareNamespace(NamespaceOf<LayoutGeometryRequest>());
        request.ShouldContainPropertyInType(
            nameof(LayoutGeometryRequest),
            nameof(LayoutGeometryRequest.ImageMetadataResolver),
            NullableTypeName<IImageMetadataResolver>());
        request.ShouldUseNamespace(NamespaceOf<IImageMetadataResolver>());

        var pageContentArea = SourceFileFor<PageContentArea>();
        pageContentArea.ShouldDeclareNamespace(NamespaceOf<PageContentArea>());
        pageContentArea.ShouldContainRecordStruct(nameof(PageContentArea), InternalAccessibility);

        var geometryGuard = SourceFileFor(typeof(GeometryGuard));
        geometryGuard.ShouldDeclareNamespace(NamespaceOf(typeof(GeometryGuard)));
        geometryGuard.ShouldContainType(nameof(GeometryGuard), InternalAccessibility);

        var tablePlacementWriter = SourceFileFor<TablePlacementWriter>();
        tablePlacementWriter.ShouldUseIdentifier(nameof(GeometryTranslator));
        tablePlacementWriter.ShouldNotInvokeMemberOn(nameof(UsedGeometry), nameof(UsedGeometry.Translate));
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
        SourceSetForTestAssemblyOf<LayoutGeometryConstruction>()
            .ShouldNotUseNamespaces(ParserPackageName());
    }

    [Fact]
    public void ParserDom_DoesNotLeakIntoHandoffContracts()
    {
        foreach (var file in new[]
                 {
                     SourceFileFor<StyleTree>(),
                     SourceFileFor<StyleNode>(),
                     SourceFileFor<StyleContentNode>(),
                     SourceFileFor<StyledElementFacts>(),
                     SourceFileFor<BoxNode>()
                 })
        {
            file.ShouldNotUseNamespaces(ParserPackageName());
            file.ShouldNotUseIdentifier(nameof(IElement));
            file.ShouldNotUseIdentifier(nameof(INode));
            file.ShouldNotUseIdentifier(nameof(IDocument));
        }
    }

    [Fact]
    public void StyleNode_HandoffCollections_AreReadOnly()
    {
        var styleNode = SourceFileFor<StyleNode>();

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
                RendererNamespace,
                AssemblyName<ITextMeasurer>(),
                ParserPackageName(),
                ExternalPackageIds.SkiaSharp);

        var fragmentPlacementCloner = SourceFileFor<FragmentPlacementCloner>();
        fragmentPlacementCloner.ShouldUseIdentifier(nameof(UsedGeometry.Translate));
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
            nameof(PaginationPlacementAudit.FragmentKind).Replace("Kind", string.Empty, StringComparison.Ordinal),
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
                     SourceSetFor<PdfRenderer>()
                 })
        {
            sourceRoot.ShouldNotUseNamespaces(NamespaceOf<RectangleF>());
            sourceRoot.ShouldNotUseIdentifiers(nameof(RectangleF), nameof(PointF), nameof(SizeF));
        }
    }

    [Fact]
    public void LayoutComposition_StaysAtStageAndHandoffBoundaries()
    {
        SemanticProjectFor<LayoutPipeline>()
            .ShouldNotReferenceNamespaces(ParserPackageName(), RendererNamespace, ExternalPackageIds.SkiaSharp);

        var layoutPipeline = SourceFileFor<LayoutPipeline>();

        layoutPipeline.ShouldContainMethodInType(nameof(LayoutPipeline), nameof(LayoutPipeline.BuildAsync),
            TaskTypeName<HtmlLayout>(), PublicAccessibility);
        layoutPipeline.ShouldNotConstructType(nameof(AngleSharpDocumentLoader));
        layoutPipeline.ShouldNotConstructType(nameof(CssStyleComputer));
        layoutPipeline.ShouldNotConstructType(nameof(BlockBoxLayout));
        layoutPipeline.ShouldNotConstructType(nameof(BlockFormattingMetricsMeasurement));
        layoutPipeline.ShouldNotConstructType(nameof(BlockPaginator));
        layoutPipeline.ShouldNotConstructType(nameof(LayoutPage));
    }

    [Fact]
    public void LayoutComposition_UsesStageFocusedRunner()
    {
        var layoutPipeline = SourceFileFor<LayoutPipeline>();
        var stageRunner = SourceFileFor<LayoutStageRunner>();
        var stageNames = SourceFileFor(typeof(LayoutStageNames));
        var snapshotDiagnostics = SourceFileFor(typeof(GeometrySnapshotDiagnostics));

        layoutPipeline.ShouldUseIdentifier(nameof(LayoutStageRunner));
        layoutPipeline.ShouldUseIdentifier(nameof(GeometrySnapshotDiagnostics));
        layoutPipeline.ShouldNotUseIdentifier(DiagnosticStageRunnerTypeName);
        layoutPipeline.ShouldNotConstructType(nameof(DiagnosticRecord));

        stageRunner.ShouldUseIdentifier(DiagnosticStageRunnerTypeName);
        stageRunner.ShouldUseIdentifier(nameof(LayoutStageNames));
        stageRunner.ShouldUseIdentifier(nameof(ILayoutGeometryStage));
        stageRunner.ShouldUseIdentifier(nameof(FragmentTreeBuilder));
        stageRunner.ShouldUseIdentifier(nameof(LayoutPaginator));

        stageNames.ShouldContainStringLiteral(LayoutStageNames.BoxTree);
        stageNames.ShouldContainStringLiteral(LayoutStageNames.FragmentTree);
        stageNames.ShouldContainStringLiteral(LayoutStageNames.Pagination);

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
                RendererNamespace,
                AssemblyName<ITextMeasurer>(),
                ParserPackageName(),
                ExternalPackageIds.SkiaSharp);

        SourceSetFor<FragmentTreeBuilder>()
            .ShouldNotUseIdentifiers(
                nameof(BoxNode),
                nameof(BoxTreeConstruction).Replace("Construction", string.Empty, StringComparison.Ordinal),
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
        var layoutGeometryConstruction = SourceFileFor<LayoutGeometryConstruction>();
        var boxTreeLayout = SourceFileFor<BoxTreeLayout>();
        var blockBoxLayout = SourceFileFor<BlockBoxLayout>();
        var blockFlow = SourceFileFor<BlockFlowLayout>();
        var standardRule = SourceFileFor<StandardBlockLayoutRule>();
        var imageRule = SourceFileFor<ImageBlockLayoutRule>();
        var ruleRule = SourceFileFor<RuleBlockLayoutRule>();
        var tableRule = SourceFileFor<TableBlockLayoutRule>();
        var tableGrid = SourceFileFor<TableGridLayout>();
        var publishedLayoutWriter = SourceFileFor<PublishedLayoutWriter>();

        layoutGeometryConstruction.ShouldUseIdentifier(nameof(BoxTreeConstruction));
        layoutGeometryConstruction.ShouldConstructType(nameof(BoxTreeLayout));
        boxTreeLayout.ShouldUseIdentifier(nameof(BlockBoxLayout));
        boxTreeLayout.ShouldUseIdentifier(nameof(PageContentArea));
        boxTreeLayout.ShouldUseIdentifier(nameof(PublishedLayoutTree));
        boxTreeLayout.ShouldUseIdentifier(nameof(BlockStackLayoutRequest));
        boxTreeLayout.ShouldNotUseIdentifier(nameof(BlockLayoutRuleSet));
        blockBoxLayout.ShouldNotUseIdentifier(nameof(BoxTreeLayout));
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockLayoutRuleSet));
        blockBoxLayout.ShouldUseIdentifier(nameof(PublishedLayoutWriter));
        blockBoxLayout.ShouldUseIdentifier(nameof(BlockSizingRules));
        blockBoxLayout.ShouldUseIdentifier(nameof(TableGridLayout));
        blockBoxLayout.ShouldNotUseIdentifier(nameof(PageContentArea));
        blockBoxLayout.ShouldNotUseIdentifier(nameof(PublishedBlockFacts));
        blockBoxLayout.ShouldNotConstructType(nameof(PublishedChildBlockItem));
        blockBoxLayout.ShouldNotConstructType(nameof(PublishedInlineFlowSegmentItem));
        blockBoxLayout.ShouldNotConstructType(nameof(PublishedInlineObjectItem));
        blockFlow.ShouldNotUseIdentifier(nameof(BlockLayoutRuleSet));
        blockFlow.ShouldNotUseIdentifier(nameof(IBlockLayoutRule));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedBlock));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedInlineLayout));
        blockFlow.ShouldNotUseIdentifier(nameof(PublishedBlockFlowItem));
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

        publishedLayoutWriter.ShouldUseIdentifier(nameof(PublishedBlockFacts));
        publishedLayoutWriter.ShouldConstructType(nameof(PublishedChildBlockItem));
        publishedLayoutWriter.ShouldConstructType(nameof(PublishedInlineFlowSegmentItem));
        publishedLayoutWriter.ShouldConstructType(nameof(PublishedInlineObjectItem));
        standardRule.ShouldNotAssignToMember(nameof(BlockBox.TextAlign));
        tableGrid.ShouldNotUseIdentifier(nameof(LayoutBoxStateWriter));
    }

    [Fact]
    public void GeometryMutableStateWrites_StayInConstructionModelsOrOwnerWriters()
    {
        var geometryRoot = SourceDirectoryFor<LayoutGeometryConstruction>();
        var allowedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            RelativeSourcePathFor<BoxTreeConstruction>(),
            RelativeSourcePathFor(typeof(BoxTreeNormalization)),
            RelativeSourcePathFor<LayoutBoxStateWriter>(),
            RelativeSourcePathFor<BlockBox>(),
            RelativeSourcePathFor<ImageBox>(),
            RelativeSourcePathFor<TableBox>(),
            RelativeSourcePathFor<TableRowBox>(),
            RelativeSourcePathFor<TableCellBox>(),
            RelativeSourcePathFor<InlineBlockBoundaryBox>()
        };
        var mutationPatterns = new[]
        {
            new Regex(
                @"\.(UsedGeometry|InlineLayout|Margin|Padding|TextAlign|DerivedColumnCount|RowIndex|ColumnIndex|ColumnSpan|IsHeader|EstablishesInlineBlockFormattingContext|Src|AuthoredSizePx|IntrinsicSizePx|Status)\s*=(?!=)",
                RegexOptions.Compiled),
            new Regex(@"\.(ApplyLayoutGeometry|ApplyImageMetadata)\s*\(", RegexOptions.Compiled)
        };

        var violations = Directory
            .GetFiles(geometryRoot, RepositoryLayout.CSharpFilePattern, SearchOption.AllDirectories)
            .Where(static path => !IsGeneratedOrBuildOutput(path))
            .Where(path => !IsAllowedMutableLayoutStateWritePath(path, allowedFiles))
            .SelectMany(path => File
                .ReadLines(path)
                .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
            .Where(item => mutationPatterns.Any(pattern => pattern.IsMatch(item.Line)))
            .Select(item => $"{RelativeSourcePath(item.Path)}:{item.Number}: {item.Line.Trim()}")
            .ToArray();

        violations.ShouldBeEmpty(
            "Mutable layout state should be assigned only by construction, model copy, or owner writer files. "
            + string.Join(" ", violations));
    }

    [Fact]
    public void GeometryChildStateMutation_UsesExplicitBoxNodeMethods()
    {
        var boxNode = SourceFileFor<BoxNode>();
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
        boxNode.ShouldContainMethodInType(
            nameof(BoxNode),
            nameof(BoxNode.AddChild),
            VoidTypeName,
            InternalAccessibility);
        boxNode.ShouldContainMethodInType(
            nameof(BoxNode),
            nameof(BoxNode.InsertChild),
            VoidTypeName,
            InternalAccessibility);
        boxNode.ShouldContainMethodInType(
            nameof(BoxNode),
            nameof(BoxNode.ReplaceChildren),
            VoidTypeName,
            InternalAccessibility);
        boxNode.ShouldContainMethodInType(
            nameof(BoxNode),
            nameof(BoxNode.ClearChildren),
            VoidTypeName,
            InternalAccessibility);

        var geometryRoot = SourceDirectoryFor<LayoutGeometryConstruction>();
        var childMutationPatterns = new[]
        {
            new Regex(@"\.Children\.(Add|Insert|Clear|AddRange|Remove|RemoveAt)\s*\(", RegexOptions.Compiled),
            new Regex(@"\.Children\[[^\]]+\]\s*=", RegexOptions.Compiled)
        };

        var violations = Directory
            .GetFiles(geometryRoot, RepositoryLayout.CSharpFilePattern, SearchOption.AllDirectories)
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
            SourceFileFor<BlockContentSizeMeasurement>(),
            SourceFileFor<BlockFormattingMetricsMeasurement>(),
            SourceFileFor<BlockFlowMeasurement>(),
            SourceFileFor<InlineFlowMeasurement>(),
            SourceFileFor<BlockSizingRules>(),
            SourceFileFor<BlockContentSizeFacts>(),
            SourceFileFor<InlineContentSizeFacts>(),
            SourceFileFor<TableCellMeasurement>(),
            SourceFileFor<TableGridLayout>(),
            SourceFileFor<AtomicInlineBoxLayout>()
        };

        SourceFileFor<BlockContentSizeMeasurement>().ShouldUseIdentifier(nameof(BlockSizingRules));
        SourceFileFor<BlockContentSizeMeasurement>().ShouldUseIdentifier(nameof(InlineContentSizeFacts));
        SourceFileFor<InlineFlowMeasurement>().ShouldNotUseIdentifier(nameof(InlineLayoutResult));
        SourceFileFor<StandardBlockLayoutRule>().ShouldUseIdentifier(nameof(BlockSizingRules));
        SourceFileFor<TableGridLayout>().ShouldUseIdentifier(nameof(BlockSizingRules));
        SourceFileFor<AtomicInlineBoxLayout>().ShouldUseIdentifier(nameof(BlockSizingRules));

        foreach (var file in measurementFiles)
        {
            file.ShouldNotUseIdentifier(nameof(LayoutBoxStateWriter));
            file.ShouldNotUseIdentifier(nameof(TableBoxStateWriter));
            file.ShouldNotUseIdentifier(nameof(InlineBoxContentStateWriter));
            file.ShouldNotUseIdentifier(nameof(PublishedLayoutWriter));
            file.ShouldNotUseIdentifier(nameof(PublishedBlockFacts));
            file.ShouldNotUseIdentifier(nameof(PublishedLayoutTree));
            file.ShouldNotInvoke(nameof(BlockBox.ApplyLayoutGeometry));
            file.ShouldNotInvoke(nameof(ImageBox.ApplyImageMetadata));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyBlockLayout));
            file.ShouldNotInvoke(nameof(LayoutBoxStateWriter.ApplyInlineLayout));
            file.ShouldNotAssignToMember(nameof(BlockBox.UsedGeometry));
            file.ShouldNotAssignToMember(nameof(BlockBox.InlineLayout));
        }
    }

    [Fact]
    public void PreLayoutMeasurement_DoesNotReadUsedGeometry()
    {
        foreach (var file in new[]
                 {
                     SourceFileFor<BlockContentSizeMeasurement>(),
                     SourceFileFor<BlockFormattingMetricsMeasurement>(),
                     SourceFileFor<BlockFlowMeasurement>(),
                     SourceFileFor<InlineFlowMeasurement>(),
                     SourceFileFor<InlineContentSizeFacts>(),
                     SourceFileFor<TableCellMeasurement>(),
                     SourceFileFor<AtomicInlineBoxLayout>()
                 })
        {
            file.ShouldNotUseIdentifier(nameof(UsedGeometry));
        }
    }

    [Fact]
    public void ProductionGeometry_UsesPrimitiveAuthoritiesForUsedGeometryTransforms()
    {
        var geometryRoot = SourceDirectoryFor<LayoutGeometryConstruction>();
        var allowedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            RelativeSourcePathFor(typeof(GeometryTranslator)),
            RelativeSourcePathFor(typeof(UsedGeometryRules))
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
            .GetFiles(geometryRoot, RepositoryLayout.CSharpFilePattern, SearchOption.AllDirectories)
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
    public void RendererSource_ConsumesRenderModelAndResourcesOnly()
    {
        SemanticProjectFor<PdfRenderer>()
            .ShouldNotReferenceNamespaces(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<StyleNode>(),
                AssemblyName<FragmentTreeBuilder>(),
                NamespaceOf<LayoutGeometryConstruction>(),
                NamespaceOf<StyleTreeBuilder>());

        SourceSetFor<PdfRenderer>()
            .ShouldNotUseIdentifiers(
                nameof(StyleTree),
                nameof(StyleNode),
                nameof(ComputedStyle),
                nameof(PublishedLayoutTree),
                nameof(FragmentTreeBuilder),
                nameof(IFontSource),
                nameof(BoxGeometrySnapshot.SourceNodeId),
                nameof(BoxGeometrySnapshot.SourceContentId),
                nameof(BoxGeometrySnapshot.SourcePath),
                nameof(BoxGeometrySnapshot.SourceElementIdentity),
                nameof(BoxGeometrySnapshot.GeneratedSourceKind),
                nameof(GeometrySourceIdentity),
                nameof(StyleSourceIdentity),
                nameof(StyleContentIdentity));
    }

    [Fact]
    public void StageOwnedSettings_AreTheInternalOptionBoundary()
    {
        var styleSettings = SourceFileFor<StyleBuildSettings>();
        styleSettings.ShouldContainType<StyleBuildSettings>(InternalAccessibility, true);
        styleSettings.ShouldContainPropertyInType<StyleBuildSettings>(
            nameof(StyleBuildSettings.UseDefaultUserAgentStyleSheet),
            CSharpTypeName<bool>(),
            PublicAccessibility);
        styleSettings.ShouldContainPropertyInType<StyleBuildSettings>(
            nameof(StyleBuildSettings.UserAgentStyleSheet),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);

        var layoutSettings = SourceFileFor<LayoutBuildSettings>();
        layoutSettings.ShouldContainType<LayoutBuildSettings>(InternalAccessibility, true);
        layoutSettings.ShouldContainPropertyInType<LayoutBuildSettings>(
            nameof(LayoutBuildSettings.Style),
            TypeName<StyleBuildSettings>(),
            PublicAccessibility);
        var pdfSettings = SourceFileFor<PdfRenderSettings>();
        pdfSettings.ShouldContainType<PdfRenderSettings>(InternalAccessibility, true);
        pdfSettings.ShouldContainPropertyInType<PdfRenderSettings>(
            nameof(PdfRenderSettings.ResourceBaseDirectory),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);
        pdfSettings.ShouldContainPropertyInType<PdfRenderSettings>(
            nameof(PdfRenderSettings.MaxImageSizeBytes),
            CSharpTypeName<long>(),
            PublicAccessibility);

        SemanticProjectFor<LayoutPipeline>()
            .ShouldNotReferenceNamespaces(FacadeOptionsNamespace);
        SemanticProjectFor<StyleTreeBuilder>()
            .ShouldNotReferenceNamespaces(FacadeOptionsNamespace);
        SemanticProjectFor<PdfRenderer>()
            .ShouldNotReferenceTypes(typeof(HtmlConverterOptions));
    }

    [Fact]
    public void FacadePublicOptions_HaveSingleOwnersForSharedConversionFacts()
    {
        var facadePublic = SemanticProjectFor<HtmlConverter>()
            .ExternallyVisibleTypeNames();
        var options = new[]
        {
            SourceFileFor<HtmlConverterOptions>(),
            SourceFileFor<PageOptions>(),
            SourceFileFor<ResourceOptions>(),
            SourceFileFor<CssOptions>(),
            SourceFileFor<FontOptions>(),
            SourceFileFor<DiagnosticsOptions>()
        };
        var htmlConverterOptions = options[0];
        var pageOptions = options[1];
        var resourceOptions = options[2];
        var fontOptions = options[4];
        var diagnosticsOptions = options[5];

        foreach (var option in options)
        {
            option.ShouldDeclareNamespace(FacadeOptionsNamespace);
        }

        facadePublic.ShouldContain(FullTypeName<HtmlConverterOptions>());
        facadePublic.ShouldContain(FullTypeName<PageOptions>());
        facadePublic.ShouldContain(FullTypeName<ResourceOptions>());
        facadePublic.ShouldContain(FullTypeName<CssOptions>());
        facadePublic.ShouldContain(FullTypeName<FontOptions>());
        facadePublic.ShouldContain(FullTypeName<DiagnosticsOptions>());

        htmlConverterOptions.ShouldContainType<HtmlConverterOptions>(PublicAccessibility, true);
        htmlConverterOptions.ShouldContainPropertyInType<HtmlConverterOptions>(
            nameof(HtmlConverterOptions.Page),
            TypeName<PageOptions>(),
            PublicAccessibility);
        htmlConverterOptions.ShouldContainPropertyInType<HtmlConverterOptions>(
            nameof(HtmlConverterOptions.Resources),
            TypeName<ResourceOptions>(),
            PublicAccessibility);
        htmlConverterOptions.ShouldContainPropertyInType<HtmlConverterOptions>(
            nameof(HtmlConverterOptions.Css),
            TypeName<CssOptions>(),
            PublicAccessibility);
        htmlConverterOptions.ShouldContainPropertyInType<HtmlConverterOptions>(
            nameof(HtmlConverterOptions.Fonts),
            TypeName<FontOptions>(),
            PublicAccessibility);
        htmlConverterOptions.ShouldContainPropertyInType<HtmlConverterOptions>(
            nameof(HtmlConverterOptions.Diagnostics),
            TypeName<DiagnosticsOptions>(),
            PublicAccessibility);
        pageOptions.ShouldContainPropertyInType<PageOptions>(
            nameof(PageOptions.Size),
            TypeName<SizePt>(),
            PublicAccessibility);
        resourceOptions.ShouldContainPropertyInType<ResourceOptions>(
            nameof(ResourceOptions.BaseDirectory),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);
        resourceOptions.ShouldContainPropertyInType<ResourceOptions>(
            nameof(ResourceOptions.MaxImageSizeBytes),
            CSharpTypeName<long>(),
            PublicAccessibility);
        fontOptions.ShouldContainPropertyInType<FontOptions>(
            nameof(FontOptions.FontPath),
            NullableCSharpTypeName<string>(),
            PublicAccessibility);
        diagnosticsOptions.ShouldContainPropertyInType<DiagnosticsOptions>(
            nameof(DiagnosticsOptions.IncludeRawHtml),
            CSharpTypeName<bool>(),
            PublicAccessibility);
        diagnosticsOptions.ShouldContainPropertyInType<DiagnosticsOptions>(
            nameof(DiagnosticsOptions.MaxRawHtmlLength),
            CSharpTypeName<int>(),
            PublicAccessibility);
    }

    [Fact]
    public void ResourceLoadingPolicy_UsesSharedResourceModuleForImages()
    {
        var resourceLoader = SourceFileFor(typeof(ImageResourceLoader));
        var resourceResult = SourceFileFor<ImageResourceResult>();
        var resourceMetadataResult = SourceFileFor<ImageResourceMetadataResult>();
        var resourceStore = SourceFileFor<ImageResourceStore>();
        var imageLoadStatus = SourceFileFor<ImageLoadStatus>();
        var metadataResult = SourceFileFor<ImageMetadataResult>();
        var publishedImageFacts = SourceFileFor<PublishedImageFacts>();
        var imageFragment = SourceFileFor<ImageFragment>();
        var imageProvider = SourceFileFor<ImageResourceMetadataResolver>();
        var imageRenderer = SourceFileFor<ImageRenderer>();

        resourceLoader.ShouldContainType(typeof(ImageResourceLoader), InternalAccessibility);
        resourceLoader.ShouldContainMethodInType(
            nameof(ImageResourceLoader),
            nameof(ImageResourceLoader.Load),
            TypeName<ImageResourceResult>(),
            PublicAccessibility);
        resourceLoader.ShouldContainMethodInType(
            nameof(ImageResourceLoader),
            nameof(ImageResourceLoader.ResolveBaseDirectory),
            CSharpTypeName<string>(),
            PublicAccessibility);
        imageLoadStatus.ShouldDeclareNamespace(NamespaceOf<ImageLoadStatus>());
        imageLoadStatus.ShouldContainEnum<ImageLoadStatus>(PublicAccessibility);
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

        resourceStore.ShouldUseIdentifier(nameof(ImageResourceLoader));
        resourceStore.ShouldUseIdentifier(nameof(ImageResourceResult));
        resourceStore.ShouldUseIdentifier(nameof(ImageResourceMetadataResult));
        imageProvider.ShouldUseIdentifier(nameof(ImageResourceStore));
        imageRenderer.ShouldUseIdentifier(nameof(IImageResourceReader));
        imageRenderer.ShouldUseIdentifier(nameof(ImageResourceLoader));
    }

    [Fact]
    public void RuntimeOptions_DoNotUseCurrentDirectoryDefaults()
    {
        foreach (var file in new[]
                 {
                     SourceFileFor<HtmlConverterOptions>(),
                     SourceFileFor<PageOptions>(),
                     SourceFileFor<ResourceOptions>(),
                     SourceFileFor<CssOptions>(),
                     SourceFileFor<FontOptions>(),
                     SourceFileFor<DiagnosticsOptions>(),
                     SourceFileFor<HtmlConverter>(),
                     SourceFileFor<LayoutBuildSettings>(),
                     SourceFileFor<LayoutGeometryRequest>(),
                     SourceFileFor<ImageSizingRules>(),
                     SourceFileFor<ImageResourceStore>(),
                     SourceFileFor<PdfRenderSettings>(),
                     SourceFileFor<ImageRenderer>(),
                     SourceFileFor(typeof(ImageResourceLoader))
                 })
        {
            file.ShouldNotUseIdentifier(nameof(Directory.GetCurrentDirectory));
        }
    }

    [Fact]
    public void StageLifecycleDiagnostics_UseCentralEmitter()
    {
        var emitter = SourceFileFor(typeof(DiagnosticStageEmitter));
        var stage = SourceFileFor(typeof(DiagnosticStageRunner));
        var converter = SourceFileFor<HtmlConverter>();
        var layoutPipeline = SourceFileFor<LayoutPipeline>();
        var styleTreeBuilder = SourceFileFor<StyleTreeBuilder>();

        foreach (var stageLifecycleEvent in StageLifecycleEvents)
        {
            emitter.ShouldContainStringLiteral(stageLifecycleEvent);
        }

        stage.ShouldUseIdentifier(nameof(DiagnosticStageEmitter));
        converter.ShouldUseIdentifier(nameof(DiagnosticStageEmitter));
        layoutPipeline.ShouldNotUseIdentifier(DiagnosticStageRunnerTypeName);
        SourceFileFor<LayoutStageRunner>().ShouldUseIdentifier(DiagnosticStageRunnerTypeName);
        styleTreeBuilder.ShouldUseIdentifier(DiagnosticStageRunnerTypeName);
        foreach (var sourceRoot in new[]
                 {
                     SourceSetFor<HtmlConverter>(),
                     SourceSetFor<LayoutPipeline>(),
                     SourceSetFor<StyleTreeBuilder>(),
                     SourceSetFor<LayoutGeometryConstruction>(),
                     SourceSetFor<LayoutPaginator>(),
                     SourceSetFor<PdfRenderer>()
                 })
        {
            sourceRoot.ShouldNotContainStringLiterals(StageLifecycleEvents);
        }
    }

    private static readonly string RendererNamespace = NamespacePrefix(
        NamespaceOf<PdfRenderer>(),
        2);

    private const string DiagnosticStageRunnerTypeName = nameof(DiagnosticStageRunner);

    private static readonly string FacadeOptionsNamespace = NamespaceOf<HtmlConverterOptions>();

    private static readonly string[] StageLifecycleEvents =
    [
        DiagnosticStageEmitter.StartedEvent,
        DiagnosticStageEmitter.SucceededEvent,
        DiagnosticStageEmitter.FailedEvent,
        DiagnosticStageEmitter.SkippedEvent,
        DiagnosticStageEmitter.CanceledEvent
    ];

    private static readonly string[] MutableLayoutStateWriterOwnerFolders =
    [
        "/BlockFlow/",
        "/Images/",
        "/InlineFlow/",
        "/Tables/",
        "/Writing/"
    ];

    private static bool IsAllowedMutableLayoutStateWritePath(
        string path,
        IReadOnlySet<string> allowedFiles)
    {
        var relativePath = RelativeSourcePath(path);
        return allowedFiles.Contains(relativePath)
               || relativePath.EndsWith("Writer.cs", StringComparison.Ordinal)
               && MutableLayoutStateWriterOwnerFolders.Any(relativePath.Contains);
    }

    private static bool IsGeneratedOrBuildOutput(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains(RepositoryLayout.BinDirectory, StringComparer.OrdinalIgnoreCase)
               || segments.Contains(RepositoryLayout.ObjDirectory, StringComparer.OrdinalIgnoreCase)
               || path.EndsWith(RepositoryLayout.GeneratedCSharpSuffix, StringComparison.Ordinal)
               || path.EndsWith(RepositoryLayout.AssemblyInfoCSharpSuffix, StringComparison.Ordinal);
    }

    private static string RelativeSourcePath(string path) =>
        Path.GetRelativePath(Paths.RepoRoot(), path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
}
