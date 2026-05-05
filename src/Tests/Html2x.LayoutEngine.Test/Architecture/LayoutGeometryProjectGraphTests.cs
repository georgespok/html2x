using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Documents;
using Html2x.Text;
using Shouldly;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryProjectGraphTests
{
    [Fact]
    public void Solution_ContainsArchitectureProjects()
    {
        ArchitectureSolution.Load("src", "Html2x.sln")
            .ProjectNames()
            .ShouldContainSet([
                AssemblyName<StyleNode>(),
                AssemblyName<HtmlLayout>(),
                AssemblyName<FragmentBuilder>(),
                AssemblyName<LayoutPaginator>(),
                TestAssemblyNameFor<LayoutPaginator>(),
                TestAssemblyNameFor<FragmentBuilder>(),
                TestAssemblyNameFor<StyleTreeBuilder>()
            ]);
    }

    [Fact]
    public void ProductionProjectGraph_FollowsOwnedModuleDirection()
    {
        ProjectFor<LayoutBuilder>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(), AssemblyName<FragmentBuilder>(), AssemblyName<LayoutGeometryBuilder>(), AssemblyName<LayoutPaginator>(), AssemblyName<StyleTreeBuilder>(), AssemblyName<HtmlLayout>(), AssemblyName<ITextMeasurer>());
        ProjectFor<StyleTreeBuilder>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(), AssemblyName<HtmlLayout>());
        ProjectFor<StyleTreeBuilder>()
            .ShouldReferencePackages(ParserPackageName(), ParserPackageName() + ".Css");
        ProjectFor<LayoutGeometryBuilder>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(), AssemblyName<HtmlLayout>(), AssemblyName<ITextMeasurer>());
        ProjectFor<LayoutPaginator>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(), AssemblyName<HtmlLayout>());
        ProjectFor<LayoutPaginator>()
            .ShouldHaveNoPackageReferences();
        ProjectFor<StyleNode>()
            .ShouldReferenceProjects(AssemblyName<HtmlLayout>());
        ProjectFor<StyleNode>()
            .ShouldHaveNoPackageReferences();
        ProjectFor<HtmlLayout>()
            .ShouldHaveNoProjectReferences();
        ProjectFor<HtmlLayout>()
            .ShouldHaveNoPackageReferences();
        ProjectFor<FragmentBuilder>()
            .ShouldReferenceProjects(AssemblyName<StyleNode>(), AssemblyName<HtmlLayout>());
        ProjectFor<ITextMeasurer>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<HtmlLayout>());
        ProjectFor<ITextMeasurer>()
            .ShouldReferencePackages(SkiaSharpPackageName, SkiaSharpPackageName + ".HarfBuzz");
    }

    [Fact]
    public void RendererProjectGraph_StaysIndependentFromLayoutStages()
    {
        var renderer = Project("src", PdfRendererAssemblyName, PdfRendererAssemblyName + ".csproj");

        renderer.ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<HtmlLayout>(), ResourcesAssemblyName, AssemblyName<ITextMeasurer>());
        renderer.ShouldNotReferenceProjects(
            AssemblyName<LayoutBuilder>(),
            AssemblyName<StyleNode>(),
            AssemblyName<FragmentBuilder>(),
            AssemblyName<LayoutGeometryBuilder>(),
            AssemblyName<StyleTreeBuilder>());
    }

    [Fact]
    public void FocusedTestProjects_StayInOwningModules()
    {
        Project("src", "Tests", TestAssemblyNameFor<LayoutPaginator>(), TestAssemblyNameFor<LayoutPaginator>() + ".csproj")
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<LayoutPaginator>(), AssemblyName<HtmlLayout>());
        Project("src", "Tests", TestAssemblyNameFor<LayoutPaginator>(), TestAssemblyNameFor<LayoutPaginator>() + ".csproj")
            .ShouldNotReferenceProjects(
                AssemblyName<LayoutBuilder>(),
                AssemblyName<FragmentBuilder>(),
                AssemblyName<LayoutGeometryBuilder>(),
                AssemblyName<StyleTreeBuilder>(),
                PdfRendererAssemblyName,
                AssemblyName<ITextMeasurer>());
        Project("src", "Tests", TestAssemblyNameFor<StyleTreeBuilder>(), TestAssemblyNameFor<StyleTreeBuilder>() + ".csproj")
            .ShouldNotReferenceProjects(AssemblyName<LayoutBuilder>(), AssemblyName<LayoutGeometryBuilder>());
        Project("src", "Tests", TestAssemblyNameFor<LayoutGeometryBuilder>(), TestAssemblyNameFor<LayoutGeometryBuilder>() + ".csproj")
            .ShouldNotReferencePackages(ParserPackageName(), ParserPackageName() + ".Css");
    }

    [Fact]
    public void RemovedCompatibilityFolders_AreAbsent()
    {
        Directory.Exists(PathFromRoot("src", AssemblyName<LayoutBuilder>(), "Pagination"))
            .ShouldBeFalse("pagination compatibility shims should not remain in the composition project.");
        Directory.Exists(PathFromRoot("src", AssemblyName<LayoutBuilder>(), "Fragment"))
            .ShouldBeFalse("fragment compatibility shims should not remain in the composition project.");
    }
}
