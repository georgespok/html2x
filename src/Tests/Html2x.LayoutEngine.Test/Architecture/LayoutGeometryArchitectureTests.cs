using Shouldly;

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
    public void FragmentProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Fragments.csproj");
    }

    [Fact]
    public void FragmentTestProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.LayoutEngine.Fragments.Test.csproj");
    }

    [Fact]
    public void ContractsProject_ReferencesOnlyAbstractions()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Html2x.LayoutEngine.Contracts.csproj");

        source.ShouldContain("Html2x.Abstractions.csproj");
        source.ShouldNotContain(ParserPackageName());
        source.ShouldNotContain("AngleSharp.Css");
        source.ShouldNotContain("SkiaSharp");
        source.ShouldNotContain("Html2x.LayoutEngine.Style.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.Geometry.csproj");
        source.ShouldNotContain("Html2x.LayoutEngine.csproj");
        source.ShouldNotContain("Html2x.Renderers.Pdf.csproj");
    }

    [Fact]
    public void FragmentProject_ReferencesOnlyAbstractionsAndContracts()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Fragments",
            "Html2x.LayoutEngine.Fragments.csproj");

        source.ShouldContain("Html2x.Abstractions.csproj");
        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
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

        source.ShouldContain("Html2x.Abstractions.csproj");
        source.ShouldContain("Html2x.LayoutEngine.Contracts.csproj");
        source.ShouldContain("Html2x.LayoutEngine.Fragments.csproj");
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
        var diagnosticsDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions", "Diagnostics");
        foreach (var file in Directory.GetFiles(diagnosticsDirectory, "*.cs"))
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(diagnosticsDirectory, file);

            source.ShouldNotContain("Html2x.LayoutEngine.Style");
            source.ShouldNotContain("Html2x.LayoutEngine.Geometry");
            source.Contains("GeometrySourceIdentity", StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not expose geometry implementation identity.");
            source.Contains("StyleSourceIdentity", StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not expose style implementation identity.");
            source.Contains("StyleContentIdentity", StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not expose style implementation identity.");
            source.Contains("StyleNodeId", StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not expose style implementation identity.");
            source.Contains("StyleContentId", StringComparison.Ordinal).ShouldBeFalse(
                $"{relativePath} should not expose style implementation identity.");
        }

        var boxGeometrySnapshot = ReadSource(
            "src",
            "Html2x.Abstractions",
            "Diagnostics",
            "BoxGeometrySnapshot.cs");

        boxGeometrySnapshot.ShouldContain("int? SourceNodeId");
        boxGeometrySnapshot.ShouldContain("int? SourceContentId");
        boxGeometrySnapshot.ShouldContain("string? SourcePath");
        boxGeometrySnapshot.ShouldContain("int? SourceOrder");
        boxGeometrySnapshot.ShouldContain("string? SourceElementIdentity");
        boxGeometrySnapshot.ShouldContain("string? GeneratedSourceKind");
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
                "Html2x.LayoutEngine.Style",
                "Html2x.LayoutEngine.Test"
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
        var pipeline = ReadSource("docs", "architecture", "pipeline.md");
        var stageOwnership = ReadSource("docs", "architecture", "stage-ownership.md");
        var testing = ReadSource("docs", "development", "testing.md");

        pipeline.ShouldContain("Html2x.LayoutEngine.Style");
        pipeline.ShouldContain("StyledElementFacts");
        pipeline.ShouldContain("StyleContentNode");
        pipeline.ShouldContain("StyleSourceIdentity");
        pipeline.ShouldContain("generated source identity");
        pipeline.ShouldContain("Published layout carries both layout identity and source identity");
        pipeline.ShouldContain("renderer-facing fragments remain independent of style implementation types");
        stageOwnership.ShouldContain("StyledElementFacts");
        stageOwnership.ShouldContain(ParserPackageName());
        stageOwnership.ShouldContain("Style owns StyleNodeId, StyleContentId, StyleSourceIdentity, and StyleContentIdentity assignment");
        stageOwnership.ShouldContain("Geometry owns BoxNode.SourceIdentity propagation and generated source identity");
        stageOwnership.ShouldContain("Phase 5 diagnostic contract");
        testing.ShouldContain("Html2x.LayoutEngine.Style.Test");
        testing.ShouldContain("Geometry tests must not reference " + ParserPackageName());
        testing.ShouldContain("unspecified identity");
        testing.ShouldContain("NodePath");
        testing.ShouldContain("SourcePath");
        pipeline.ShouldContain("Html2x.LayoutEngine.Contracts");
        pipeline.ShouldContain("Html2x.LayoutEngine.Fragments");
        pipeline.ShouldContain("PublishedLayoutTree and IFontSource");
        pipeline.ShouldContain("Fragment projection does not consume mutable boxes");
        stageOwnership.ShouldContain("Html2x.LayoutEngine.Contracts owns internal pipeline handoff contracts");
        stageOwnership.ShouldContain("Html2x.LayoutEngine.Fragments owns published layout traversal");
        stageOwnership.ShouldContain("Renderers do not reference fragment projection");
        testing.ShouldContain("Html2x.LayoutEngine.Contracts");
        testing.ShouldContain("Html2x.LayoutEngine.Fragments.Test");
        testing.ShouldContain("Fragment projection tests build PublishedLayoutTree inputs directly");
        testing.ShouldContain("Fragment projection tests must not construct mutable boxes");
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
