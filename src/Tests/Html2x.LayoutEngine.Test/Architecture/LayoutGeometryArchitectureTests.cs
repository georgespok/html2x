using Shouldly;
using Html2x.RenderModel;
using System.Text.RegularExpressions;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryArchitectureTests
{
    [Fact]
    public void GeometryProject_DoesNotReferenceLayoutEngineProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj");

        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
    }

    [Fact]
    public void LayoutEngineProject_ReferencesGeometryProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Geometry.csproj");
    }

    [Fact]
    public void LayoutEngineProject_ReferencesStyleProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Style.csproj");
    }

    [Fact]
    public void LayoutEngineProject_ReferencesContractsProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
    }

    [Fact]
    public void LayoutEngineProject_ReferencesFragmentProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Fragments.csproj");
    }

    [Fact]
    public void LayoutEngineProject_ReferencesPaginationProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Pagination.csproj");
    }

    [Fact]
    public void StyleProject_DoesNotReferenceLayoutEngineOrGeometryProjects()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj");

        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
    }

    [Fact]
    public void StyleProject_ReferencesContractsProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
    }

    [Fact]
    public void GeometryProject_ReferencesContractsProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
    }

    [Fact]
    public void GeometryProject_DoesNotReferenceAbstractionsProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj");

        source.ShouldNotContain("Html2x.Abstractions.csproj");
    }

    [Fact]
    public void LayoutEngineProject_DoesNotReferenceAbstractionsProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj");

        source.ShouldNotContain("Html2x.Abstractions.csproj");
    }

    [Fact]
    public void StyleProject_DoesNotReferenceAbstractionsProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj");

        source.ShouldNotContain("Html2x.Abstractions.csproj");
    }

    [Fact]
    public void GeometryProject_DoesNotReferenceStyleProject()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj");

        source.ShouldNotContain("Html2x.LayoutEngine.Style.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Style");
    }

    [Fact]
    public void GeometryProject_DoesNotReferenceParserPackage()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj");

        source.ShouldNotContain(ParserPackageName());
    }

    [Fact]
    public void LayoutEngineProject_DoesNotReferenceParserPackage()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj");

        source.ShouldNotContain(ParserPackageName());
    }

    [Fact]
    public void ContractsProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
    }

    [Fact]
    public void RenderModelProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.RenderModel.csproj");
    }

    [Fact]
    public void FragmentProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Fragments.csproj");
    }

    [Fact]
    public void PaginationProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Pagination.csproj");
    }

    [Fact]
    public void PaginationTestProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Pagination.Test.csproj");
    }

    [Fact]
    public void FragmentTestProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Fragments.Test.csproj");
    }

    [Fact]
    public void PaginationProject_ReferencesOnlyApprovedProjects()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Pagination",
            "Html2x.LayoutEngine.Pagination.csproj");

        source.ShouldContain("Html2x.Diagnostics.Contracts.csproj");
        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
        source.ShouldContain("Html2x.RenderModel.csproj");
        source.ShouldNotContain("Html2x.Abstractions.csproj");
        source.ShouldNotContain("Html2x.Diagnostics.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Fragments.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Style.csproj");
        source.ShouldNotContain("Html2x.Renderers.Pdf.csproj");
        source.ShouldNotContain("Html2x.Text.csproj");
        source.ShouldNotContain(ParserPackageName());
        source.ShouldNotContain("SkiaSharp");
        CountOccurrences(source, "<ProjectReference Include=").ShouldBe(3);
    }

    [Fact]
    public void PaginationTestProject_ReferencesOnlyApprovedProjects()
    {
        var source = ReadSource(
            "src",
            "Tests",
            "Html2x.LayoutEngine.Pagination.Test",
            "Html2x.LayoutEngine.Pagination.Test.csproj");

        source.ShouldContain("coverlet.collector");
        source.ShouldContain("Shouldly");
        source.ShouldContain("Microsoft.NET.Test.Sdk");
        source.ShouldContain("xunit");
        source.ShouldContain("Html2x.Diagnostics.Contracts.csproj");
        source.ShouldContain("Html2x.LayoutEngine.Pagination.csproj");
        source.ShouldContain("Html2x.RenderModel.csproj");
        source.ShouldNotContain("Html2x.Abstractions.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Contracts.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Fragments.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Style.csproj");
        source.ShouldNotContain("Html2x.Renderers.Pdf.csproj");
        source.ShouldNotContain("Html2x.Text.csproj");
        source.ShouldNotContain(ParserPackageName());
        source.ShouldNotContain("SkiaSharp");
        CountOccurrences(source, "<ProjectReference Include=").ShouldBe(3);
    }

    [Fact]
    public void ContractsProject_ReferencesOnlyRenderModel()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Html2x.LayoutEngine.Contracts.csproj");

        source.ShouldContain("Html2x.RenderModel.csproj");
        source.ShouldNotContain("Html2x.Abstractions.csproj");
        source.ShouldNotContain(ParserPackageName());
        source.ShouldNotContain("AngleSharp.Css");
        source.ShouldNotContain("SkiaSharp");
        source.ShouldNotContain("Html2x.LayoutEngine.Style.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
        source.ShouldNotContain("Html2x.Renderers.Pdf.csproj");
        CountOccurrences(source, "<ProjectReference Include=").ShouldBe(1);
    }

    [Fact]
    public void AbstractionsProject_IsRemovedFromSourceAndSolution()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions");
        var solution = ReadSource("src", "Html2x.sln");

        Directory.Exists(directory).ShouldBeFalse("the obsolete options-only module should be deleted.");
        solution.ShouldNotContain("Html2x.Abstractions");
    }

    [Fact]
    public void DeletedAbstractionsProject_CannotOwnImageMetadataContracts()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions");

        Directory.Exists(directory).ShouldBeFalse("image metadata contracts should stay in layout contracts after deleting the obsolete module.");
    }

    [Fact]
    public void GeometryContractsProject_OwnsImageMetadataContracts()
    {
        var directory = Path.Combine(
            FindRepoRoot(),
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Geometry",
            "Images");
        var request = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Geometry",
            "LayoutGeometryRequest.cs");

        File.Exists(Path.Combine(directory, "IImageMetadataResolver.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(directory, "ImageMetadataResult.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(directory, "ImageMetadataStatus.cs")).ShouldBeTrue();
        request.ShouldContain("IImageMetadataResolver? ImageMetadataResolver");
        request.ShouldContain("Html2x.LayoutEngine.Geometry.Images");
    }

    [Fact]
    public void DeletedAbstractionsProject_CannotOwnDocumentOrFragmentModels()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions");

        Directory.Exists(directory).ShouldBeFalse("document and fragment render facts should stay in RenderModel after deleting the obsolete module.");
    }

    [Fact]
    public void AbstractionsProject_DoesNotOwnGeometryGuards()
    {
        var abstractionsDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions");
        var geometryDirectory = Path.Combine(abstractionsDirectory, "Layout", "Geometry");
        var geometryGuard = ReadSource(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Geometry",
            "GeometryGuard.cs");

        Directory.Exists(geometryDirectory).ShouldBeFalse(
            "Geometry validation helpers belong to Html2x.LayoutEngine.Geometry, not public options.");
        geometryGuard.ShouldContain("namespace Html2x.LayoutEngine.Geometry;");
        geometryGuard.ShouldContain("internal static class GeometryGuard");
    }

    [Fact]
    public void ContractsProject_OwnsPageContentAreaFact()
    {
        var contractsDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Contracts");
        var geometryDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Geometry");
        var contractsPageContentArea = Path.Combine(contractsDirectory, "Geometry", "PageContentArea.cs");
        var geometryPageContentArea = Path.Combine(geometryDirectory, "Geometry", "PageContentArea.cs");

        File.Exists(contractsPageContentArea).ShouldBeTrue();
        File.Exists(geometryPageContentArea).ShouldBeFalse(
            "Page content area is a layout-owned fact shared by geometry and pagination, not a geometry implementation helper.");
        var source = File.ReadAllText(contractsPageContentArea);
        source.ShouldContain("namespace Html2x.LayoutEngine.Contracts.Geometry;");
        source.ShouldContain("internal readonly record struct PageContentArea");
    }

    [Fact]
    public void RenderModelProject_OwnsRenderFactTranslationHelpers()
    {
        var renderModelDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.RenderModel");
        var helperPath = Path.Combine(renderModelDirectory, "Geometry", "RenderGeometryTranslator.cs");
        var helperSource = File.ReadAllText(helperPath);

        File.Exists(helperPath).ShouldBeTrue();
        helperSource.ShouldContain("public static class RenderGeometryTranslator");
        helperSource.ShouldContain("RectangleF Translate(");
        helperSource.ShouldContain("TextRun Translate(");

        var geometryTranslator = ReadSource(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Geometry",
            "GeometryTranslator.cs");
        geometryTranslator.ShouldNotContain("TextRun Translate(");
        geometryTranslator.ShouldNotContain("RectangleF Translate(");
    }

    [Fact]
    public void PaginationSource_UsesRenderFactTranslationHelperInsteadOfGeometryTranslator()
    {
        var paginationDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Pagination");

        foreach (var file in Directory.GetFiles(paginationDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(paginationDirectory, file);
            var source = File.ReadAllText(file);

            Regex.IsMatch(source, @"(?<![A-Za-z])GeometryTranslator\b").ShouldBeFalse(
                $"{relativePath} should use render fact translation helpers, not geometry implementation helpers.");
        }

        ReadSource("src", "Html2x.LayoutEngine.Pagination", "FragmentPlacementCloner.cs")
            .ShouldContain("RenderGeometryTranslator");
    }

    [Fact]
    public void PaginationProject_ExposesLayoutPaginatorAndKeepsBlockPaginatorInternal()
    {
        var layoutPaginator = ReadSource("src", "Html2x.LayoutEngine.Pagination", "LayoutPaginator.cs");
        var blockPaginator = ReadSource("src", "Html2x.LayoutEngine.Pagination", "BlockPaginator.cs");
        var layoutBuilder = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");

        layoutPaginator.ShouldContain("public sealed class LayoutPaginator");
        layoutPaginator.ShouldContain("public PaginationResult Paginate(");
        layoutPaginator.ShouldContain("PaginationOptions options");
        blockPaginator.ShouldContain("internal sealed class BlockPaginator");
        blockPaginator.ShouldNotContain("public sealed class BlockPaginator");
        layoutBuilder.ShouldContain("new LayoutPaginator()");
        layoutBuilder.ShouldNotContain("new BlockPaginator()");
    }

    [Fact]
    public void PaginationResult_OwnsLayoutAndAuditFacts()
    {
        var models = ReadSource("src", "Html2x.LayoutEngine.Pagination", "PaginationModels.cs");

        models.ShouldContain("public sealed class PaginationOptions");
        models.ShouldContain("public sealed class PaginationResult");
        models.ShouldContain("public required HtmlLayout Layout");
        models.ShouldContain("public required IReadOnlyList<PaginationPageAudit> AuditPages");
        models.ShouldContain("public enum PaginationDecisionKind");
        models.ShouldContain("MovedToNextPage");
        models.ShouldContain("SplitAcrossPages");
        models.ShouldContain("ForcedBreak");
        models.ShouldContain("public sealed class PaginationPlacementAudit");
        models.ShouldContain("public required RectangleF PlacedRect");
        models.ShouldContain("internal sealed class BlockFragmentPlacement");
        models.ShouldNotContain("public sealed class BlockFragmentPlacement");

        var auditStart = models.IndexOf("public sealed class PaginationPlacementAudit", StringComparison.Ordinal);
        var resultStart = models.IndexOf("public sealed class PaginationResult", StringComparison.Ordinal);
        var auditSource = models[auditStart..resultStart];
        auditSource.ShouldNotContain("BlockFragment Fragment");
    }

    [Fact]
    public void LayoutBuilder_DoesNotOwnPaginationPageAssembly()
    {
        var layoutBuilder = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");

        layoutBuilder.ShouldContain("new PaginationOptions");
        layoutBuilder.ShouldContain("return pagination.Layout;");
        layoutBuilder.ShouldNotContain("new LayoutPage(");
        layoutBuilder.ShouldNotContain("CreateLayoutPageChildren");
        layoutBuilder.ShouldNotContain("pagination.Pages");
    }

    [Fact]
    public void GeometrySnapshotMapper_ConsumesPaginationAuditFactsWithoutCloneImplementationNames()
    {
        var mapper = ReadSource("src", "Html2x.LayoutEngine", "Diagnostics", "GeometrySnapshotMapper.cs");

        mapper.ShouldContain("pagination.AuditPages");
        mapper.ShouldContain("PaginationPageAudit");
        mapper.ShouldContain("PaginationPlacementAudit");
        mapper.ShouldContain("DecisionKind");
        mapper.ShouldNotContain("FragmentPlacementCloner");
        mapper.ShouldNotContain("BlockFragmentPlacement");
    }

    [Fact]
    public void PaginationProductionSource_DoesNotReferenceForbiddenModules()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Pagination");
        var forbiddenTokens = new[]
        {
            "Html2x.Abstractions",
            "Html2x.LayoutEngine.Geometry",
            "Html2x.LayoutEngine.Fragments",
            "Html2x.LayoutEngine.Style",
            "Html2x.Renderers",
            "Html2x.Text",
            ParserPackageName(),
            "SkiaSharp"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath) ||
                relativePath.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should stay behind the pagination seam and use only render facts, diagnostics contracts, and layout-owned contracts.");
            }
        }
    }

    [Fact]
    public void PaginationTestSource_DoesNotReferenceForbiddenModules()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Tests", "Html2x.LayoutEngine.Pagination.Test");
        var forbiddenTokens = new[]
        {
            "Html2x.Abstractions",
            "Html2x.LayoutEngine.Fragments",
            "Html2x.LayoutEngine.Geometry",
            "Html2x.LayoutEngine.Style",
            "Html2x.LayoutEngine.Test",
            "Html2x.Renderers",
            "Html2x.Text",
            ParserPackageName(),
            "SkiaSharp"
        };

        foreach (var file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath) ||
                !IsSourceOrProjectFile(file))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should test pagination through render facts and pagination contracts only.");
            }
        }
    }

    [Fact]
    public void LayoutEngineTestProject_DoesNotOwnFocusedPaginationTests()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Tests", "Html2x.LayoutEngine.Test");
        var oldPaginationDirectory = Path.Combine(directory, "Pagination");
        var forbiddenTokens = new[]
        {
            "namespace Html2x.LayoutEngine.Test." + "Pagination",
            "class " + "LayoutPaginatorTests",
            "class " + "FragmentPlacementClonerTests",
            "new " + "FragmentPlacementCloner(",
            "CloneBlock" + "WithPlacement("
        };

        Directory.Exists(oldPaginationDirectory).ShouldBeFalse(
            "Focused pagination behavior tests belong in Html2x.LayoutEngine.Pagination.Test.");

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath) ||
                relativePath.StartsWith("Architecture", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep pagination behavior ownership in Html2x.LayoutEngine.Pagination.Test.");
            }
        }
    }

    [Fact]
    public void PaginationProductionSource_DoesNotContainCompatibilityShimsUnderOldProject()
    {
        var layoutEngineDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine");
        var oldPaginationDirectory = Path.Combine(layoutEngineDirectory, "Pagination");
        var forbiddenTokens = new[]
        {
            "namespace Html2x.LayoutEngine." + "Pagination;",
            "class " + "LayoutPaginator",
            "class " + "BlockPaginator",
            "class " + "FragmentPlacementCloner"
        };

        Directory.Exists(oldPaginationDirectory).ShouldBeFalse(
            "Pagination production code should live only in Html2x.LayoutEngine.Pagination.");

        foreach (var file in Directory.GetFiles(layoutEngineDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(layoutEngineDirectory, file);
            if (IsBuildOutputPath(relativePath))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should not keep pagination compatibility shims in the old project.");
            }
        }
    }

    [Fact]
    public void RenderModelProject_OwnsPureValueAndFontFacts()
    {
        var renderModelDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.RenderModel");
        File.Exists(Path.Combine(renderModelDirectory, "Text", "FontKey.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Text", "FontWeight.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Text", "FontStyle.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Text", "ResolvedFont.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Measurements", "Units", "SizePx.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Measurements", "Units", "SizePt.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Measurements", "Units", "PaperSizes.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Styles", "ColorRgba.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Styles", "VisualStyle.cs")).ShouldBeTrue();

        var productionDirectories = Directory.GetDirectories(Path.Combine(FindRepoRoot(), "src"))
            .Where(static directory =>
                !Path.GetFileName(directory).Equals("Html2x.RenderModel", StringComparison.Ordinal) &&
                !Path.GetFileName(directory).Equals("Tests", StringComparison.Ordinal));
        var forbiddenDefinitions = new[]
        {
            "record FontKey",
            "class FontKey",
            "enum FontWeight",
            "enum FontStyle",
            "record ResolvedFont",
            "record struct SizePx",
            "record struct SizePt",
            "class PaperSizes",
            "record struct ColorRgba",
            "record VisualStyle"
        };

        foreach (var directory in productionDirectories)
        {
            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(FindRepoRoot(), file);
                if (IsBuildOutputPath(Path.GetRelativePath(directory, file)))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                foreach (var token in forbiddenDefinitions)
                {
                    source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                        $"{relativePath} should not define render model fact token `{token}` outside Html2x.RenderModel.");
                }
            }
        }
    }

    [Fact]
    public void RenderModelProject_OwnsDocumentAndFragmentFacts()
    {
        var renderModelDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.RenderModel");
        File.Exists(Path.Combine(renderModelDirectory, "Documents", "HtmlLayout.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Documents", "LayoutPage.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Documents", "LayoutMetadata.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "Fragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "BlockFragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "LineBoxFragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "TextRun.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "ImageFragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "RuleFragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "TableFragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "TableRowFragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "TableCellFragment.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "FragmentMetadata.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(renderModelDirectory, "Fragments", "FragmentGeometryGuard.cs")).ShouldBeTrue();

        var productionDirectories = Directory.GetDirectories(Path.Combine(FindRepoRoot(), "src"))
            .Where(static directory =>
                !Path.GetFileName(directory).Equals("Html2x.RenderModel", StringComparison.Ordinal) &&
                !Path.GetFileName(directory).Equals("Tests", StringComparison.Ordinal));
        var forbiddenDefinitions = new[]
        {
            "class HtmlLayout",
            "record LayoutPage",
            "record LayoutMetadata",
            "abstract class Fragment",
            "class BlockFragment :",
            "class LineBoxFragment :",
            "record TextRun(",
            "class ImageFragment :",
            "class RuleFragment :",
            "class TableFragment :",
            "class TableRowFragment :",
            "class TableCellFragment :",
            "enum FragmentDisplayRole",
            "enum FormattingContextKind"
        };

        foreach (var directory in productionDirectories)
        {
            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(FindRepoRoot(), file);
                if (IsBuildOutputPath(Path.GetRelativePath(directory, file)))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                foreach (var token in forbiddenDefinitions)
                {
                    source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                        $"{relativePath} should not define document or fragment render model token `{token}` outside Html2x.RenderModel.");
                }
            }
        }
    }

    [Fact]
    public void RenderModelProject_IsPureFactsOnly()
    {
        var project = ReadSource("src", "Html2x.RenderModel", "Html2x.RenderModel.csproj");

        project.ShouldNotContain("<ProjectReference");
        project.ShouldNotContain("<PackageReference");

        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.RenderModel");
        var forbiddenTokens = new[]
        {
            "SkiaSharp",
            "System.IO",
            "IFileDirectory",
            "ISkiaTypefaceFactory",
            "Html2x.Text",
            "Html2x.Abstractions",
            "Html2x.Diagnostics",
            "Html2x.LayoutEngine",
            "Html2x.Renderers",
            ParserPackageName()
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath) ||
                relativePath.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep Html2x.RenderModel free of runtime adapters and implementation modules.");
            }
        }
    }

    [Fact]
    public void RenderModelProject_FriendAssemblies_AreExplicitAndLimited()
    {
        var source = ReadSource(
            "src",
            "Html2x.RenderModel",
            "Properties",
            "InternalsVisibleTo.cs");

        ExtractFriendAssemblies(source)
            .ShouldBe(["Html2x.LayoutEngine.Fragments"], ignoreOrder: true);
    }

    [Fact]
    public void TextProject_ReferencesOnlyRenderModelDiagnosticsContractsAndSkiaPackages()
    {
        var source = ReadSource("src", "Html2x.Text", "Html2x.Text.csproj");

        source.ShouldContain("Html2x.RenderModel.csproj");
        source.ShouldContain("Html2x.Diagnostics.Contracts.csproj");
        source.ShouldContain("PackageReference Include=\"SkiaSharp\"");
        source.ShouldContain("PackageReference Include=\"SkiaSharp.HarfBuzz\"");
        source.ShouldNotContain("Html2x.Abstractions.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Contracts.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Fragments.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Style.csproj");
        source.ShouldNotContain("Html2x.Renderers.Pdf.csproj");
        CountOccurrences(source, "<ProjectReference Include=").ShouldBe(2);
    }

    [Fact]
    public void TextProject_FriendAssemblies_AreExplicitAndLimited()
    {
        var source = ReadSource(
            "src",
            "Html2x.Text",
            "Properties",
            "InternalsVisibleTo.cs");

        ExtractFriendAssemblies(source)
            .ShouldBe(["Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.Test"], ignoreOrder: true);
    }

    [Fact]
    public void TextImplementationHelpers_AreNotPublicSurface()
    {
        var fontDirectoryIndex = ReadSource("src", "Html2x.Text", "FontDirectoryIndex.cs");
        var fontFaceEntry = ReadSource("src", "Html2x.Text", "FontFaceEntry.cs");
        var fileDirectory = ReadSource("src", "Html2x.Text", "FileDirectory.cs");
        var fileDirectoryInterface = ReadSource("src", "Html2x.Text", "IFileDirectory.cs");
        var typefaceFactory = ReadSource("src", "Html2x.Text", "SkiaTypefaceFactory.cs");
        var typefaceFactoryInterface = ReadSource("src", "Html2x.Text", "ISkiaTypefaceFactory.cs");
        var fontPathSource = ReadSource("src", "Html2x.Text", "FontPathSource.cs");
        var skiaTextMeasurer = ReadSource("src", "Html2x.Text", "SkiaTextMeasurer.cs");

        fontDirectoryIndex.ShouldContain("internal static class FontDirectoryIndex");
        fontDirectoryIndex.ShouldNotContain("public static class FontDirectoryIndex");
        fontFaceEntry.ShouldContain("internal sealed record FontFaceEntry");
        fontFaceEntry.ShouldNotContain("public sealed record FontFaceEntry");
        fileDirectory.ShouldContain("internal sealed class FileDirectory");
        fileDirectory.ShouldNotContain("public sealed class FileDirectory");
        fileDirectoryInterface.ShouldContain("internal interface IFileDirectory");
        fileDirectoryInterface.ShouldNotContain("public interface IFileDirectory");
        typefaceFactory.ShouldContain("internal sealed class SkiaTypefaceFactory");
        typefaceFactory.ShouldNotContain("public sealed class SkiaTypefaceFactory");
        typefaceFactoryInterface.ShouldContain("internal interface ISkiaTypefaceFactory");
        typefaceFactoryInterface.ShouldNotContain("public interface ISkiaTypefaceFactory");
        fontPathSource.ShouldContain("public FontPathSource(string fontPath)");
        fontPathSource.ShouldNotContain("public FontPathSource(string fontPath, IFileDirectory");
        fontPathSource.ShouldNotContain("public FontPathSource(string fontPath, IFileDirectory fileDirectory, ISkiaTypefaceFactory");
        skiaTextMeasurer.ShouldContain("public SkiaTextMeasurer(IFontSource fontSource)");
        skiaTextMeasurer.ShouldNotContain("public SkiaTextMeasurer(IFontSource fontSource, IFileDirectory");
    }

    [Fact]
    public void TextProductionSource_DoesNotReferenceLayoutEngineOrRendererNamespaces()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Text");
        var forbiddenTokens = new[]
        {
            "Html2x.Abstractions",
            "Html2x.LayoutEngine",
            "Html2x.Renderers"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath) ||
                relativePath.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep Html2x.Text independent from `{token}`.");
            }
        }
    }

    [Fact]
    public void FragmentProject_ReferencesOnlyContractsAndRenderModel()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Fragments",
            "Html2x.LayoutEngine.Fragments.csproj");

        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
        source.ShouldContain("Html2x.RenderModel.csproj");
        source.ShouldNotContain("Html2x.Abstractions.csproj");
        source.ShouldNotContain("Html2x.Text.csproj");
        CountOccurrences(source, "<ProjectReference Include=").ShouldBe(2);
    }

    [Fact]
    public void FragmentProject_DoesNotReferenceCompositionStyleGeometryRenderersOrParsers()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Fragments",
            "Html2x.LayoutEngine.Fragments.csproj");
        var forbiddenTokens = new[]
        {
            "Html2x.LayoutEngine.csproj",
            "Html2x.LayoutEngine.Geometry.csproj",
            "Html2x.LayoutEngine.Style.csproj",
            "Html2x.Renderers.Pdf.csproj",
            ParserPackageName(),
            ParserPackageName() + ".Css",
            "SkiaSharp"
        };

        foreach (var token in forbiddenTokens)
        {
            source.ShouldNotContain(token);
        }
    }

    [Fact]
    public void FragmentTestProject_DoesNotReferenceCompositionStyleGeometryRenderersOrParsers()
    {
        var source = ReadSource(
            "src",
            "Tests",
            "Html2x.LayoutEngine.Fragments.Test",
            "Html2x.LayoutEngine.Fragments.Test.csproj");
        var forbiddenTokens = new[]
        {
            "Html2x.LayoutEngine.csproj",
            "Html2x.LayoutEngine.Geometry.csproj",
            "Html2x.LayoutEngine.Style.csproj",
            "Html2x.Renderers.Pdf.csproj",
            ParserPackageName(),
            ParserPackageName() + ".Css",
            "SkiaSharp"
        };

        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
        source.ShouldContain("Html2x.LayoutEngine.Fragments.csproj");
        source.ShouldContain("Html2x.RenderModel.csproj");
        source.ShouldNotContain("Html2x.Abstractions.csproj");
        CountOccurrences(source, "<ProjectReference Include=").ShouldBe(3);
        foreach (var token in forbiddenTokens)
        {
            source.ShouldNotContain(token);
        }
    }

    [Fact]
    public void ContractsSource_DoesNotUseParserRendererOrMutableBoxTypes()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Contracts");
        var forbiddenTokens = new[]
        {
            ParserUsingToken(),
            ParserDomToken(),
            ParserElementInterfaceName(),
            ParserNodeInterfaceName(),
            ParserDocumentInterfaceName(),
            "Html2x.Renderers",
            "SkiaSharp",
            "BoxNode",
            "BlockBox",
            "InlineBox",
            "TableBox",
            "ImageBox",
            "RuleBox",
            "BlockLayoutEngine",
            "InlineLayoutEngine",
            "TableLayoutEngine",
            "InitialBoxTreeBuilder"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (relativePath.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep contracts free of parser, renderer, and mutable box implementation token `{token}`.");
            }
        }
    }

    [Fact]
    public void GeometryProductionSource_DoesNotUseParserNamespaces()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Geometry");

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            source.Contains(ParserUsingToken(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use the parser namespace.");
            source.Contains(ParserDomToken(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser DOM types.");
            source.Contains(ParserElementInterfaceName(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser element types.");
            source.Contains(ParserNodeInterfaceName(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser node types.");
            source.Contains(ParserDocumentInterfaceName(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser document types.");
        }
    }

    [Fact]
    public void StyleAndBoxNodes_DoNotExposeParserDom()
    {
        var styleTree = ReadSource("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleTree.cs");
        var styleNode = ReadSource("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleNode.cs");
        var styleContentNode = ReadSource("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleContentNode.cs");
        var styledElementFacts = ReadSource("src", "Html2x.LayoutEngine.Contracts", "Style", "StyledElementFacts.cs");
        var boxNode = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Models", "BoxNode.cs");

        styleTree.ShouldNotContain(ParserPackageName());
        styleTree.ShouldNotContain(ParserDomToken());
        styleTree.ShouldNotContain(ParserElementInterfaceName());
        styleTree.ShouldNotContain(ParserNodeInterfaceName());
        styleTree.ShouldNotContain(ParserDocumentInterfaceName());
        styleNode.ShouldNotContain(ParserPackageName());
        styleNode.ShouldNotContain(ParserDomToken());
        styleNode.ShouldNotContain(ParserElementInterfaceName());
        styleNode.ShouldNotContain(ParserNodeInterfaceName());
        styleNode.ShouldNotContain(ParserDocumentInterfaceName());
        styleContentNode.ShouldNotContain(ParserPackageName());
        styleContentNode.ShouldNotContain(ParserDomToken());
        styleContentNode.ShouldNotContain(ParserElementInterfaceName());
        styleContentNode.ShouldNotContain(ParserNodeInterfaceName());
        styleContentNode.ShouldNotContain(ParserDocumentInterfaceName());
        styledElementFacts.ShouldNotContain(ParserPackageName());
        styledElementFacts.ShouldNotContain(ParserDomToken());
        styledElementFacts.ShouldNotContain(ParserElementInterfaceName());
        styledElementFacts.ShouldNotContain(ParserNodeInterfaceName());
        styledElementFacts.ShouldNotContain(ParserDocumentInterfaceName());
        boxNode.ShouldNotContain(ParserPackageName());
        boxNode.ShouldNotContain(ParserDomToken());
        boxNode.ShouldNotContain(ParserElementInterfaceName());
        boxNode.ShouldNotContain(ParserNodeInterfaceName());
        boxNode.ShouldNotContain(ParserDocumentInterfaceName());
    }

    [Fact]
    public void StyleNode_HandoffCollections_AreReadOnly()
    {
        var styleNode = ReadSource("src", "Html2x.LayoutEngine.Contracts", "Style", "StyleNode.cs");

        styleNode.ShouldNotContain("public List<StyleNode> Children");
        styleNode.ShouldNotContain("public List<StyleContentNode> Content");
        styleNode.ShouldContain("IReadOnlyList<StyleNode> Children");
        styleNode.ShouldContain("IReadOnlyList<StyleContentNode> Content");

        var geometryDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Geometry");
        foreach (var file in Directory.GetFiles(geometryDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(geometryDirectory, file);

            source.Contains("styleNode.Children.Add", StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not mutate style handoff children.");
            source.Contains("styleNode.Content.Add", StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not mutate style handoff content.");
        }
    }

    [Fact]
    public void StyleTestProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Style.Test.csproj");
    }

    [Fact]
    public void StyleTestProject_DoesNotReferenceLayoutEngineOrGeometryProjects()
    {
        var source = ReadSource(
            "src",
            "Tests",
            "Html2x.LayoutEngine.Style.Test",
            "Html2x.LayoutEngine.Style.Test.csproj");

        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
    }

    [Fact]
    public void GeometryTestProject_DoesNotReferenceParserPackages()
    {
        var source = ReadSource(
            "src",
            "Tests",
            "Html2x.LayoutEngine.Geometry.Test",
            "Html2x.LayoutEngine.Geometry.Test.csproj");

        source.ShouldNotContain(ParserPackageName());
    }

    [Fact]
    public void GeometryTestSource_DoesNotUseParserNamespaces()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Tests", "Html2x.LayoutEngine.Geometry.Test");

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            source.Contains(ParserUsingToken(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use the parser namespace.");
            source.Contains(ParserDomToken(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser DOM types.");
            source.Contains(ParserElementInterfaceName(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser element types.");
            source.Contains(ParserNodeInterfaceName(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser node types.");
            source.Contains(ParserDocumentInterfaceName(), StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not use parser document types.");
        }
    }

    [Fact]
    public void LayoutEngineTests_DoNotConstructStyleImplementationDirectly()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Tests", "Html2x.LayoutEngine.Test");
        var forbiddenTokens = new[]
        {
            "new " + ParserDomProviderName(),
            "new " + StyleComputerTypeName()
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (relativePath.StartsWith("Architecture", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should exercise style through the module facade.");
            }
        }
    }

    [Fact]
    public void LayoutEngineProductionSource_ComposesThroughStyleFacade()
    {
        var layoutBuilder = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");

        layoutBuilder.ShouldContain("IStyleTreeBuilder");
        layoutBuilder.ShouldContain("StyleTreeBuilder");
        layoutBuilder.ShouldNotContain("new " + ParserDomProviderName());
        layoutBuilder.ShouldNotContain("new " + StyleComputerTypeName());
    }

    [Fact]
    public void LayoutEngineProductionSource_DoesNotConstructParserOrStyleImplementationDirectly()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine");
        var forbiddenTokens = new[]
        {
            "new " + ParserDomProviderName(),
            "new " + StyleComputerTypeName()
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should compose style through IStyleTreeBuilder or StyleTreeBuilder.");
            }
        }
    }

    [Fact]
    public void StyleSource_DoesNotReferenceRendererNamespaces()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Style");

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            source.ShouldNotContain("Html2x.Renderers");
            source.ShouldNotContain("SkiaSharp");
        }
    }

    [Fact]
    public void PdfRendererProject_DoesNotReferenceStyleOrGeometryProjects()
    {
        var source = ReadSource("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj");

        source.ShouldNotContain("Html2x.LayoutEngine.Contracts.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Style.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
    }

    [Fact]
    public void PdfRendererProject_DoesNotReferenceAbstractionsProject()
    {
        var source = ReadSource("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj");

        source.ShouldNotContain("Html2x.Abstractions.csproj");
    }

    [Fact]
    public void PdfRendererProject_DoesNotReferenceFragmentProjectionProject()
    {
        var source = ReadSource("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj");

        source.ShouldNotContain("Html2x.LayoutEngine.Fragments.csproj");
    }

    [Fact]
    public void PdfRendererSource_DoesNotReferenceStyleImplementation()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Renderers.Pdf");
        var forbiddenTokens = new[]
        {
            "Html2x.LayoutEngine.Contracts",
            "Html2x.LayoutEngine.Style",
            StyleComputerTypeName(),
            "StyleTraversal",
            "StyledElementFacts",
            "StyleNode",
            "StyleTree"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should render from HtmlLayout and renderer-facing fragments.");
            }
        }
    }

    [Fact]
    public void PdfRendererSource_DoesNotReferenceFragmentProjectionImplementation()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Renderers.Pdf");
        var forbiddenTokens = new[]
        {
            "Html2x.LayoutEngine.Fragments",
            "FragmentBuilder",
            "PublishedLayoutTree",
            "PublishedBlock"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should render from HtmlLayout and renderer-facing fragments.");
            }
        }
    }

    [Fact]
    public void PdfRendererSource_DoesNotReferenceFontSourceSeam()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Renderers.Pdf");
        var forbiddenTokens = new[]
        {
            "IFontSource",
            "RendererFallbackFontResolver"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should consume resolved font facts instead of resolving fonts.");
            }
        }
    }

    [Fact]
    public void InternalStageProductionSource_DoesNotConsumePublicOptionTypes()
    {
        var directories = new[]
        {
            Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine"),
            Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Style"),
            Path.Combine(FindRepoRoot(), "src", "Html2x.Renderers.Pdf")
        };
        var forbiddenTokens = new[]
        {
            "Html2x.Abstractions",
            "HtmlConverterOptions",
            "LayoutOptions",
            "PdfOptions",
            "DiagnosticsOptions"
        };

        foreach (var directory in directories)
        {
            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(directory, file);
                if (IsBuildOutputPath(relativePath) ||
                    relativePath.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                foreach (var token in forbiddenTokens)
                {
                    source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                        $"{relativePath} should consume stage-owned settings instead of public option token `{token}`.");
                }
            }
        }
    }

    [Fact]
    public void StageOwnedSettings_AreTheInternalOptionBoundary()
    {
        var styleSettings = ReadSource("src", "Html2x.LayoutEngine.Style", "StyleBuildSettings.cs");
        var layoutSettings = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuildSettings.cs");
        var pdfSettings = ReadSource("src", "Html2x.Renderers.Pdf", "PdfRenderSettings.cs");
        var converter = ReadSource("src", "Html2x", "HtmlConverter.cs");

        styleSettings.ShouldContain("public sealed class StyleBuildSettings");
        styleSettings.ShouldContain("UseDefaultUserAgentStyleSheet");
        styleSettings.ShouldContain("UserAgentStyleSheet");
        layoutSettings.ShouldContain("public sealed class LayoutBuildSettings");
        layoutSettings.ShouldContain("StyleBuildSettings");
        layoutSettings.ShouldContain("MaxImageSizeBytes");
        pdfSettings.ShouldContain("public sealed class PdfRenderSettings");
        pdfSettings.ShouldContain("HtmlDirectory");
        converter.ShouldContain("ToLayoutBuildSettings(HtmlConverterOptions options)");
        converter.ShouldContain("ToPdfRenderSettings(HtmlConverterOptions options)");
        converter.ShouldContain("PageSize = options.Page.Size");
        converter.ShouldContain("HtmlDirectory = options.Resources.BaseDirectory");
        converter.ShouldContain("MaxImageSizeBytes = options.Resources.MaxImageSizeBytes");
        converter.ShouldContain("UseDefaultUserAgentStyleSheet = options.Css.UseDefaultUserAgentStyleSheet");
        converter.ShouldContain("UserAgentStyleSheet = options.Css.UserAgentStyleSheet");
        converter.ShouldContain("new StyleBuildSettings");
        converter.ShouldContain("new PdfRenderSettings");
        converter.ShouldNotContain("LayoutOptions");
        converter.ShouldNotContain("PdfOptions");
    }

    [Fact]
    public void Html2xProject_OwnsActivePublicOptionsAndDoesNotReferenceAbstractions()
    {
        var project = ReadSource("src", "Html2x", "Html2x.csproj");
        var options = ReadSource("src", "Html2x", "Options", "HtmlConverterOptions.cs");

        project.ShouldNotContain("Html2x.Abstractions.csproj");
        options.ShouldContain("namespace Html2x;");
        options.ShouldContain("public sealed class HtmlConverterOptions");
        options.ShouldContain("public PageOptions Page");
        options.ShouldContain("public ResourceOptions Resources");
        options.ShouldContain("public CssOptions Css");
        options.ShouldContain("public FontOptions Fonts");
        options.ShouldContain("public DiagnosticsOptions Diagnostics");
        options.ShouldNotContain("LayoutOptions");
        options.ShouldNotContain("PdfOptions");
        options.ShouldNotContain("PdfLicenseType");
        options.ShouldNotContain("LicenseType");
        options.ShouldNotContain("EnableDebugging");
        options.ShouldNotContain("MaxImageSizeMb");
    }

    [Fact]
    public void FacadePublicOptions_HaveSingleOwnersForSharedConversionFacts()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x", "Options");
        var source = string.Join(
            Environment.NewLine,
            Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        CountOccurrences(source, "public SizePt Size").ShouldBe(1);
        CountOccurrences(source, "public string BaseDirectory").ShouldBe(1);
        CountOccurrences(source, "public long MaxImageSizeBytes").ShouldBe(1);
        CountOccurrences(source, "public string? FontPath").ShouldBe(1);
        source.ShouldNotContain("PageSize");
        source.ShouldNotContain("HtmlDirectory");
        source.ShouldNotContain("MaxImageSizeMb");
    }

    [Fact]
    public void PdfRendererSource_TextDependency_IsLimitedToTypefaceLoadingSeams()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Renderers.Pdf");
        var allowedTextTokens = new[]
        {
            "using Html2x.Text;",
            "IFileDirectory",
            "ISkiaTypefaceFactory",
            "new FileDirectory()",
            "new SkiaTypefaceFactory()"
        };
        var forbiddenTokens = new[]
        {
            "IFontSource",
            "ITextMeasurer",
            "FontPathSource",
            "SkiaTextMeasurer",
            "DiagnosticsFontSource"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should not resolve fonts through text source seams.");
            }

            if (!source.Contains("Html2x.Text", StringComparison.Ordinal) &&
                !source.Contains("IFileDirectory", StringComparison.Ordinal) &&
                !source.Contains("ISkiaTypefaceFactory", StringComparison.Ordinal) &&
                !source.Contains("FileDirectory", StringComparison.Ordinal) &&
                !source.Contains("SkiaTypefaceFactory", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var textToken in Regex.Matches(source, @"Html2x\.Text|IFileDirectory|ISkiaTypefaceFactory|FileDirectory|SkiaTypefaceFactory")
                         .Select(static match => match.Value)
                         .Distinct(StringComparer.Ordinal))
            {
                allowedTextTokens.Any(token => token.Contains(textToken, StringComparison.Ordinal)).ShouldBeTrue(
                    $"{relativePath} should limit text dependency token `{textToken}` to renderer typeface loading seams.");
            }
        }
    }

    [Fact]
    public void PdfRenderer_PublicSurface_DoesNotExposeRuntimeAdapters()
    {
        var source = ReadSource("src", "Html2x.Renderers.Pdf", "Pipeline", "PdfRenderer.cs");

        source.ShouldContain("public PdfRenderer()");
        source.ShouldNotContain("public PdfRenderer(IFileDirectory");
        source.ShouldNotContain("public PdfRenderer(ISkiaTypefaceFactory");
    }

    [Fact]
    public void Composition_DoesNotConstructLowLevelTextRuntimeSeams()
    {
        var source = ReadSource("src", "Html2x", "HtmlConverter.cs");

        source.ShouldContain("new FontPathSource(fontPath)");
        source.ShouldContain("new SkiaTextMeasurer(fontSource)");
        source.ShouldNotContain("new FileDirectory()");
        source.ShouldNotContain("new SkiaTypefaceFactory()");
    }

    [Fact]
    public void LayoutBuilder_UsesLayoutGeometryInterfaceInsteadOfMutableBoxTree()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");

        source.ShouldContain("LayoutGeometryBuilder");
        source.ShouldNotContain("BoxTreeBuilder");
        source.ShouldNotContain("BuildInitial(");
    }

    [Fact]
    public void LayoutBuilder_DoesNotCallGeometryImplementationEnginesDirectly()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");

        source.ShouldContain("LayoutGeometryBuilder");
        foreach (var token in new[]
        {
            "BlockLayoutEngine",
            "InlineLayoutEngine",
            "TableLayoutEngine",
            "InitialBoxTreeBuilder",
            "BoxTreeBuilder"
        })
        {
            source.ShouldNotContain(token);
        }
    }

    [Fact]
    public void LayoutBuilder_DoesNotExposeUnusedFontSourceAdapter()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");

        source.ShouldNotContain("IFontSource fontSource");
        source.ShouldNotContain("ArgumentNullException.ThrowIfNull(fontSource)");
    }

    [Fact]
    public void LayoutGeometryBuilder_PublicBuildInterface_StartsFromStyleTree()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Geometry",
            "LayoutGeometryBuilder.cs");

        source.ShouldContain("public PublishedLayoutTree Build(");
        source.ShouldContain("StyleTree styles");
        source.ShouldNotContain("public PublishedLayoutTree Build(\r\n        BoxNode");
        source.ShouldNotContain("public PublishedLayoutTree Build(\n        BoxNode");
    }

    [Fact]
    public void GeometryPublicSurface_DoesNotExposeMutableBoxOrLayoutImplementationTypes()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Geometry");
        var forbiddenDeclarations = new[]
        {
            "public sealed class BlockLayoutEngine",
            "public sealed class InlineLayoutEngine",
            "public sealed class TableLayoutEngine",
            "public sealed class InitialBoxTreeBuilder",
            "public abstract class BoxNode",
            "public class BlockBox",
            "public sealed class InlineBox",
            "public sealed class FloatBox",
            "public sealed class ImageBox",
            "public sealed class RuleBox",
            "public sealed class TableBox",
            "public sealed class TableSectionBox",
            "public sealed class TableRowBox",
            "public sealed class TableCellBox",
            "public sealed class InlineBlockBoundaryBox",
            "public sealed class BoxTree",
            "public enum BoxRole",
            "public sealed class PageBox",
            "public sealed record InlineLayoutResult",
            "public sealed record InlineFlowSegmentLayout",
            "public sealed record InlineLineLayout",
            "public abstract record InlineLineItemLayout",
            "public sealed record InlineTextItemLayout",
            "public sealed record InlineObjectItemLayout",
            "public readonly record struct InlineLayoutRequest",
            "public sealed class TableLayoutResult",
            "public sealed record TableLayoutRowResult",
            "public sealed record TableLayoutCellPlacement",
            "public static class TableLayoutDiagnostics",
            "public sealed class FontMetricsProvider",
            "public sealed class DefaultLineHeightStrategy",
            "public interface ILineHeightStrategy"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            foreach (var declaration in forbiddenDeclarations)
            {
                source.Contains(declaration, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep `{declaration}` internal to the geometry module.");
            }
        }
    }

    [Fact]
    public void PublishedGeometryFacts_DoNotReferenceBoxImplementationNamespace()
    {
        var directory = Path.Combine(
            FindRepoRoot(),
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Published");

        foreach (var file in Directory.GetFiles(directory, "*.cs"))
        {
            var source = File.ReadAllText(file);
            source.Contains("Html2x.LayoutEngine.Box", StringComparison.Ordinal).ShouldBeFalse(
                $"{Path.GetFileName(file)} should keep published facts independent from the box Implementation namespace.");
        }
    }

    [Fact]
    public void GeometrySourceIdentity_KeepsSourceIdentitySeparateFromLayoutPath()
    {
        var boxNode = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Models", "BoxNode.cs");
        var initialBoxTreeBuilder = ReadSource(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Box",
            "InitialBoxTreeBuilder.cs");
        var publishedBlockIdentity = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Published",
            "PublishedBlockIdentity.cs");
        var publishedInlineLayout = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Published",
            "PublishedInlineLayout.cs");
        var publishedBlockFactory = ReadSource(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Box",
            "Publishing",
            "PublishedBlockFactory.cs");

        boxNode.ShouldContain("GeometrySourceIdentity SourceIdentity");
        initialBoxTreeBuilder.ShouldContain("styleNode.Identity");
        initialBoxTreeBuilder.ShouldContain("content.Identity");
        publishedBlockIdentity.ShouldContain("string NodePath");
        publishedBlockIdentity.ShouldContain("GeometrySourceIdentity SourceIdentity");
        publishedInlineLayout.ShouldContain("string NodePath");
        publishedInlineLayout.ShouldContain("GeometrySourceIdentity SourceIdentity");
        publishedBlockFactory.ShouldContain("new PublishedBlockIdentity(");
        publishedBlockFactory.ShouldContain("new PublishedInlineSource(");
        publishedBlockFactory.ShouldContain("source.SourceIdentity");
    }

    [Fact]
    public void SourceIdentity_AssignmentBoundary_StaysBeforeGeometry()
    {
        var styleTraversal = ReadSource("src", "Html2x.LayoutEngine.Style", "Style", "StyleTraversal.cs");
        var initialBoxTreeBuilder = ReadSource(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Box",
            "InitialBoxTreeBuilder.cs");

        styleTraversal.ShouldContain("new StyleSourceIdentity(");
        styleTraversal.ShouldContain("new StyleContentIdentity(");
        initialBoxTreeBuilder.ShouldContain("styleNode.Identity");
        initialBoxTreeBuilder.ShouldContain("content.Identity");
        initialBoxTreeBuilder.ShouldNotContain("new StyleSourceIdentity(");
        initialBoxTreeBuilder.ShouldNotContain("new StyleContentIdentity(");
        initialBoxTreeBuilder.ShouldNotContain("SourcePath");

        var geometryDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Geometry");
        var forbiddenSourcePathConstructionTokens = new[]
        {
            "querySelector",
            "SelectorText",
            "CssSelector",
            "CreatePathSegment"
        };

        foreach (var file in Directory.GetFiles(geometryDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(geometryDirectory, file);

            foreach (var token in forbiddenSourcePathConstructionTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should not rebuild source paths from parser or CSS selectors.");
            }
        }
    }

    [Fact]
    public void DiagnosticsSourceIdentity_UsesPrimitiveSnapshotFieldsOnly()
    {
        var boxGeometrySnapshot = ReadSource(
            "src",
            "Html2x.LayoutEngine",
            "Diagnostics",
            "DiagnosticSnapshots.cs");

        boxGeometrySnapshot.ShouldNotContain("Html2x.LayoutEngine.Style");
        boxGeometrySnapshot.ShouldNotContain("Html2x.LayoutEngine.Geometry");
        boxGeometrySnapshot.Contains("GeometrySourceIdentity", StringComparison.Ordinal).ShouldBeFalse(
            "Diagnostic snapshots should expose primitive source identity fields only.");
        boxGeometrySnapshot.Contains("StyleSourceIdentity", StringComparison.Ordinal).ShouldBeFalse(
            "Diagnostic snapshots should expose primitive source identity fields only.");
        boxGeometrySnapshot.Contains("StyleContentIdentity", StringComparison.Ordinal).ShouldBeFalse(
            "Diagnostic snapshots should expose primitive source identity fields only.");
        boxGeometrySnapshot.Contains("StyleNodeId", StringComparison.Ordinal).ShouldBeFalse(
            "Diagnostic snapshots should expose primitive source identity fields only.");
        boxGeometrySnapshot.Contains("StyleContentId", StringComparison.Ordinal).ShouldBeFalse(
            "Diagnostic snapshots should expose primitive source identity fields only.");
        boxGeometrySnapshot.ShouldContain("int? SourceNodeId");
        boxGeometrySnapshot.ShouldContain("int? SourceContentId");
        boxGeometrySnapshot.ShouldContain("string? SourcePath");
        boxGeometrySnapshot.ShouldContain("int? SourceOrder");
        boxGeometrySnapshot.ShouldContain("string? SourceElementIdentity");
        boxGeometrySnapshot.ShouldContain("string? GeneratedSourceKind");
    }

    [Fact]
    public void AbstractionsProject_DoesNotOwnStyleDimensionContracts()
    {
        var abstractionsDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions");

        Directory.Exists(abstractionsDirectory).ShouldBeFalse(
            "style-owned dimension contracts should stay in the style module after deleting the obsolete module.");

        var dimensionStyleMapper = ReadSource(
            "src",
            "Html2x.LayoutEngine.Style",
            "Style",
            "DimensionStyleMapper.cs");
        dimensionStyleMapper.ShouldContain("using Html2x.LayoutEngine.Style.Dimensions;");
    }

    [Fact]
    public void RendererSource_DoesNotReferenceSourceIdentityImplementation()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Renderers.Pdf");
        var forbiddenTokens = new[]
        {
            "SourceNodeId",
            "SourceContentId",
            "SourcePath",
            "SourceElementIdentity",
            "GeneratedSourceKind",
            "GeometrySourceIdentity",
            "StyleSourceIdentity",
            "StyleContentIdentity",
            "StyleNodeId",
            "StyleContentId"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should not reference source identity implementation token `{token}`.");
            }
        }
    }

    [Fact]
    public void GeometryProject_FriendAssemblies_AreExplicitAndLimited()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Geometry",
            "Properties",
            "InternalsVisibleTo.cs");

        ExtractFriendAssemblies(source)
            .OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal)
            .ToArray()
            .ShouldBe(new[]
            {
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Geometry.Test",
                "Html2x.LayoutEngine.Test"
            }.OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void LayoutEngineProject_FriendAssemblies_AreExplicitAndLimited()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine",
            "Properties",
            "InternalsVisibleTo.cs");

        ExtractFriendAssemblies(source)
            .OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal)
            .ToArray()
            .ShouldBe(new[]
            {
                "Html2x.LayoutEngine.Geometry.Test",
                "Html2x.LayoutEngine.Test"
            }.OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void FragmentProject_FriendAssemblies_AreExplicitAndLimited()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Fragments",
            "Properties",
            "InternalsVisibleTo.cs");

        ExtractFriendAssemblies(source)
            .OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal)
            .ToArray()
            .ShouldBe(new[]
            {
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Fragments.Test",
                "Html2x.LayoutEngine.Geometry.Test",
                "Html2x.LayoutEngine.Test"
            }.OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void ContractsProject_FriendAssemblies_AreExplicitAndLimited()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Properties",
            "InternalsVisibleTo.cs");

        ExtractFriendAssemblies(source)
            .OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal)
            .ToArray()
            .ShouldBe(new[]
            {
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Fragments.Test",
                "Html2x.LayoutEngine.Geometry",
                "Html2x.LayoutEngine.Geometry.Test",
                "Html2x.LayoutEngine.Pagination",
                "Html2x.LayoutEngine.Style",
                "Html2x.LayoutEngine.Test"
            }.OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void PaginationProject_FriendAssemblies_AreExplicitAndLimited()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Pagination",
            "Properties",
            "InternalsVisibleTo.cs");

        ExtractFriendAssemblies(source)
            .OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal)
            .ToArray()
            .ShouldBe(new[]
            {
                "Html2x.LayoutEngine.Pagination.Test"
            }.OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void ContractsProject_FriendAssemblies_IncludeFragmentModuleAndTests()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Properties",
            "InternalsVisibleTo.cs");
        var assemblies = ExtractFriendAssemblies(source);

        assemblies.ShouldContain("Html2x.LayoutEngine.Fragments");
        assemblies.ShouldContain("Html2x.LayoutEngine.Fragments.Test");
    }

    [Fact]
    public void LayoutGeometryRequest_IsOwnedByContractsProject()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Geometry",
            "LayoutGeometryRequest.cs");

        source.ShouldContain("namespace Html2x.LayoutEngine.Geometry;");
    }

    [Fact]
    public void FragmentProductionSource_UsesCanonicalNamespace()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Fragments");

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath) ||
                relativePath.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = File.ReadAllText(file);

            source.ShouldContain("namespace Html2x.LayoutEngine.Fragments;");
            source.ShouldNotContain("namespace Html2x.LayoutEngine.Fragment;");
            source.ShouldNotContain("using Html2x.LayoutEngine.Fragment;");
        }
    }

    [Fact]
    public void FragmentCompatibilityShim_IsNotPresent()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine", "Fragment");

        Directory.Exists(directory).ShouldBeFalse(
            "Fragment compatibility shims should not be added unless public API compatibility requires them.");
    }

    [Fact]
    public void FragmentBuilderAndFragmentTree_PublicSurface_IsPreserved()
    {
        var builder = ReadSource("src", "Html2x.LayoutEngine.Fragments", "FragmentBuilder.cs");
        var tree = ReadSource("src", "Html2x.LayoutEngine.Fragments", "FragmentTree.cs");

        builder.ShouldContain("public sealed class FragmentBuilder");
        builder.ShouldContain("internal FragmentTree Build(");
        builder.ShouldNotContain("public FragmentTree Build(");
        tree.ShouldContain("public sealed class FragmentTree");
    }

    [Fact]
    public void PublishedLayoutNamespace_CleanupIsDeferred()
    {
        var directory = Path.Combine(
            FindRepoRoot(),
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Published");

        foreach (var file in Directory.GetFiles(directory, "*.cs"))
        {
            var source = File.ReadAllText(file);
            source.ShouldContain("namespace Html2x.LayoutEngine.Geometry.Published;");
        }
    }

    [Fact]
    public void FragmentProductionSource_DoesNotReferenceMutableBoxOrImplementationTypes()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Fragments");
        var forbiddenTokens = new[]
        {
            "Html2x.LayoutEngine.Box",
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
            "InitialBoxTreeBuilder"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath))
            {
                continue;
            }

            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should consume published geometry facts, not mutable box or implementation token `{token}`.");
            }
        }
    }

    [Fact]
    public void FragmentProductionSource_DoesNotReferenceFontSourceSeam()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine.Fragments");

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath))
            {
                continue;
            }

            source.ShouldNotContain("IFontSource");
        }
    }

    [Fact]
    public void LayoutEngineProductionSources_DoNotReferenceGeometryBoxImplementationNamespace()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.LayoutEngine");

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            source.Contains("using Html2x.LayoutEngine.Box", StringComparison.Ordinal).ShouldBeFalse(
                $"{Path.GetRelativePath(directory, file)} should not reference the geometry box Implementation namespace.");
            source.Contains("new BlockLayoutEngine", StringComparison.Ordinal).ShouldBeFalse(
                $"{Path.GetRelativePath(directory, file)} should compose through LayoutGeometryBuilder.");
        }
    }

    [Fact]
    public void GeometrySnapshotMapper_ConsumesPublishedLayoutTreeInsteadOfMutableBoxTree()
    {
        var source = ReadSource("src", "Html2x.LayoutEngine", "Diagnostics", "GeometrySnapshotMapper.cs");

        source.ShouldContain("PublishedLayoutTree layoutTree");
        source.ShouldNotContain("BoxTree");
    }

    [Fact]
    public void ArchitectureDocs_DescribeDeepModuleBoundaries()
    {
        var overview = ReadSource("docs", "architecture", "overview.md");
        var pipeline = ReadSource("docs", "architecture", "pipeline.md");
        var stageOwnership = ReadSource("docs", "architecture", "stage-ownership.md");
        var geometry = ReadSource("docs", "architecture", "geometry.md");
        var pagination = ReadSource("docs", "internals", "pagination.md");
        var diagnosticsEvents = ReadSource("docs", "reference", "diagnostics-events.md");
        var testing = ReadSource("docs", "development", "testing.md");

        overview.ShouldContain("Html2x.LayoutEngine.Pagination");
        overview.ShouldContain("Returns `PaginationResult` with final `HtmlLayout` and pagination audit facts");
        overview.ShouldContain("Pipeline composition from style, geometry, fragment projection, and pagination");
        overview.ShouldContain("public-to-stage settings mapping");
        overview.ShouldContain("facade-owned options");
        overview.ShouldContain("Html2x.LayoutEngine.Pagination -> PaginationResult");
        pipeline.ShouldContain("Html2x.LayoutEngine.Style");
        pipeline.ShouldContain("Html2x.RenderModel");
        pipeline.ShouldContain("Html2x.Text");
        pipeline.ShouldContain("Html2x.LayoutEngine.Pagination");
        pipeline.ShouldContain("consumes render model block fragments and returns PaginationResult");
        pipeline.ShouldContain("owns translated fragment clones and page assembly");
        pipeline.ShouldContain("does not use style, geometry implementation engines, fragment projection, parser packages, renderers, or SkiaSharp");
        pipeline.ShouldContain("returns `PaginationResult`");
        pipeline.ShouldContain("block-boundary only");
        pipeline.ShouldContain("owns public converter options");
        pipeline.ShouldContain("maps public options into stage-owned settings and requests");
        pipeline.ShouldContain("No standalone options module sits between the facade and stages");
        pipeline.ShouldContain("configuration stays with `Html2x`");
        pipeline.ShouldContain("single public conversion request");
        pipeline.ShouldContain("consumes StyleBuildSettings instead of public options");
        pipeline.ShouldContain("consumes PdfRenderSettings instead of public converter options");
        pipeline.ShouldContain("internal stage request");
        pipeline.ShouldContain("Geometry validation helpers stay inside the geometry module");
        pipeline.ShouldContain("owns pure render facts");
        pipeline.ShouldContain("does not use facade options, layout engine projects, or renderers");
        pipeline.ShouldContain("Production composition uses high-level text construction");
        pipeline.ShouldContain("`FontKey`, `FontWeight`, `FontStyle`, and `ResolvedFont` live in");
        pipeline.ShouldContain("StyledElementFacts");
        pipeline.ShouldContain("StyleContentNode");
        pipeline.ShouldContain("StyleSourceIdentity");
        pipeline.ShouldContain("generated source identity");
        pipeline.ShouldContain("Published layout carries both layout identity and source identity");
        pipeline.ShouldContain("Renderer-facing documents and fragments remain independent of style");
        pipeline.ShouldContain("that module after geometry publishes layout facts");
        pipeline.ShouldContain("models live in `Html2x.RenderModel`");
        stageOwnership.ShouldContain("StyledElementFacts");
        stageOwnership.ShouldContain(ParserPackageName());
        stageOwnership.ShouldContain("| Pagination | `Html2x.LayoutEngine.Pagination`");
        stageOwnership.ShouldContain("`PaginationResult` with final `HtmlLayout` and audit facts");
        stageOwnership.ShouldContain("Translated fragment clones, `LayoutPage` assembly, page audit facts, placement audit facts, and pagination diagnostics");
        stageOwnership.ShouldContain("Source fragments, mutable boxes, style facts, geometry implementation engines, fragment projection, parser state, renderer state");
        stageOwnership.ShouldContain("Its public entry");
        stageOwnership.ShouldContain("point is `LayoutPaginator`, not `BlockPaginator`");
        stageOwnership.ShouldContain("splits only at block boundaries");
        stageOwnership.ShouldContain("`PaginationResult.Layout`");
        stageOwnership.ShouldContain("Html2x.RenderModel owns pure render facts");
        stageOwnership.ShouldContain("Public Facade");
        stageOwnership.ShouldContain("single public conversion request");
        stageOwnership.ShouldContain("Page size has one public owner");
        stageOwnership.ShouldContain("must not pass public option objects into style, layout, pagination, or");
        stageOwnership.ShouldContain("StyleBuildSettings");
        stageOwnership.ShouldContain("PdfRenderSettings");
        stageOwnership.ShouldContain("public converter options");
        stageOwnership.ShouldContain("Geometry owns geometry validation helpers");
        stageOwnership.ShouldContain("`HtmlLayout`, `LayoutPage`");
        stageOwnership.ShouldContain("Style owns StyleNodeId, StyleContentId, StyleSourceIdentity, and StyleContentIdentity assignment");
        stageOwnership.ShouldContain("Style owns CSS dimension request and resolution facts");
        stageOwnership.ShouldContain("Html2x.Text owns text measurement contracts and font resolution contracts");
        stageOwnership.ShouldContain("Html2x.RenderModel`, `Html2x.Diagnostics.Contracts`, `SkiaSharp`, and");
        stageOwnership.ShouldContain("Low-level runtime seams such as `IFileDirectory` and `ISkiaTypefaceFactory`");
        stageOwnership.ShouldContain("internal implementation details");
        stageOwnership.ShouldContain("does not resolve fonts through `IFontSource`");
        stageOwnership.ShouldContain("must not define `ITextMeasurer`");
        stageOwnership.ShouldContain("Font identity primitives, `FontKey`, `FontWeight`, and `FontStyle`, are owned by");
        stageOwnership.ShouldContain("`Html2x.RenderModel`");
        stageOwnership.ShouldContain("Geometry owns BoxNode.SourceIdentity propagation and generated source identity");
        stageOwnership.ShouldContain("Geometry owns image metadata resolution as a layout input");
        stageOwnership.ShouldContain("generic diagnostic fields");
        testing.ShouldContain("Html2x.LayoutEngine.Style.Test");
        testing.ShouldContain("Geometry tests must not reference " + ParserPackageName());
        testing.ShouldContain("unspecified identity");
        testing.ShouldContain("NodePath");
        testing.ShouldContain("SourcePath");
        pipeline.ShouldContain("Html2x.LayoutEngine.Contracts");
        pipeline.ShouldContain("Html2x.LayoutEngine.Fragments");
        pipeline.ShouldContain("PublishedLayoutTree in, FragmentTree out");
        pipeline.ShouldContain("Text runs must carry `ResolvedFont` facts before rendering");
        pipeline.ShouldContain("Image metadata contracts live in `Html2x.LayoutEngine.Contracts`");
        pipeline.ShouldContain("Fragment projection does not consume mutable boxes");
        stageOwnership.ShouldContain("Html2x.LayoutEngine.Contracts owns internal pipeline handoff contracts");
        stageOwnership.ShouldContain("Html2x.LayoutEngine.Fragments owns published layout traversal");
        stageOwnership.ShouldContain("render model fragments");
        stageOwnership.ShouldContain("Renderers do not reference fragment projection");
        geometry.ShouldContain("Render fact translation is owned by `Html2x.RenderModel`");
        geometry.ShouldContain("`RenderGeometryTranslator`");
        geometry.ShouldContain("`UsedGeometry` translation remains geometry-owned");
        geometry.ShouldContain("Page content area calculation is a layout-owned fact");
        geometry.ShouldContain("`PageContentArea` is shared by geometry and");
        pagination.ShouldContain("`LayoutPaginator`: public pagination module");
        pagination.ShouldContain("`PaginationResult`: final `HtmlLayout` plus stable audit facts");
        pagination.ShouldContain("`BlockPaginator`: current first-fit block-boundary algorithm");
        pagination.ShouldContain("The current algorithm is intentionally block-boundary only");
        pagination.ShouldContain("not split text lines, images, table rows, or paragraphs internally");
        diagnosticsEvents.ShouldContain("stage `stage/pagination`");
        diagnosticsEvents.ShouldContain("`PaginationDecisionKind` values");
        diagnosticsEvents.ShouldContain("current pagination remains");
        testing.ShouldContain("Html2x.LayoutEngine.Contracts");
        testing.ShouldContain("Html2x.LayoutEngine.Fragments.Test");
        testing.ShouldContain("Html2x.LayoutEngine.Pagination.Test");
        testing.ShouldContain("Focused pagination behavior through `LayoutPaginator`");
        testing.ShouldContain("focused pagination behavior belongs in `Html2x.LayoutEngine.Pagination.Test`");
        testing.ShouldContain("They must not reference style, geometry implementation");
        testing.ShouldContain("fragment projection, text runtime seams, parser packages, renderers");
        testing.ShouldContain("Fragment projection tests build PublishedLayoutTree inputs directly");
        testing.ShouldContain("Font resolution behavior belongs");
        testing.ShouldContain("Fake text measurers must implement `Measure`");
        testing.ShouldContain("Renderer tests that manually construct `TextRun` values must include");
        testing.ShouldContain("Fragment projection tests must not construct mutable boxes");
        testing.ShouldNotContain("Pagination tests remain in `Html2x.LayoutEngine.Test`");
    }

    private static string ReadSource(params string[] pathSegments)
    {
        return File.ReadAllText(Path.Combine([FindRepoRoot(), .. pathSegments]));
    }

    private static string ParserPackageName() => "Angle" + "Sharp";

    private static string ParserUsingToken() => "using " + ParserPackageName();

    private static string ParserDomToken() => ParserPackageName() + ".Dom";

    private static string ParserElementInterfaceName() => "I" + "Element";

    private static string ParserNodeInterfaceName() => "I" + "Node";

    private static string ParserDocumentInterfaceName() => "I" + "Document";

    private static string ParserDomProviderName() => ParserPackageName() + "DomProvider";

    private static string StyleComputerTypeName() => "CssStyle" + "Computer";

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while (index < source.Length)
        {
            index = source.IndexOf(value, index, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            index += value.Length;
        }

        return count;
    }

    private static bool IsBuildOutputPath(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Length > 0 &&
            (segments[0].Equals("bin", StringComparison.OrdinalIgnoreCase) ||
             segments[0].Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSourceOrProjectFile(string path)
    {
        var extension = Path.GetExtension(path);

        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ExtractFriendAssemblies(string source)
    {
        const string marker = "InternalsVisibleTo(\"";
        var assemblies = new List<string>();
        var index = 0;

        while (index < source.Length)
        {
            var start = source.IndexOf(marker, index, StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            start += marker.Length;
            var end = source.IndexOf('"', start);
            if (end < 0)
            {
                break;
            }

            assemblies.Add(source[start..end]);
            index = end + 1;
        }

        return assemblies;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Html2x.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
