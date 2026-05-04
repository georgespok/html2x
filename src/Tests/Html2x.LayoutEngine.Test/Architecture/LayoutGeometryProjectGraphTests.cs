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
                "Html2x.LayoutEngine.Contracts",
                "Html2x.RenderModel",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Pagination",
                "Html2x.LayoutEngine.Pagination.Test",
                "Html2x.LayoutEngine.Fragments.Test",
                "Html2x.LayoutEngine.Style.Test"
            ]);
    }

    [Fact]
    public void ProductionProjectGraph_FollowsOwnedModuleDirection()
    {
        Project("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj")
            .ShouldReferenceProjects([
                "Html2x.Diagnostics.Contracts",
                "Html2x.LayoutEngine.Contracts",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Geometry",
                "Html2x.LayoutEngine.Pagination",
                "Html2x.LayoutEngine.Style",
                "Html2x.RenderModel",
                "Html2x.Text"
            ]);
        Project("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj")
            .ShouldReferenceProjects(["Html2x.Diagnostics.Contracts", "Html2x.LayoutEngine.Contracts", "Html2x.RenderModel"]);
        Project("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj")
            .ShouldReferencePackages([ParserPackageName(), ParserPackageName() + ".Css"]);
        Project("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj")
            .ShouldReferenceProjects(["Html2x.Diagnostics.Contracts", "Html2x.LayoutEngine.Contracts", "Html2x.RenderModel", "Html2x.Text"]);
        Project("src", "Html2x.LayoutEngine.Pagination", "Html2x.LayoutEngine.Pagination.csproj")
            .ShouldReferenceProjects(["Html2x.Diagnostics.Contracts", "Html2x.LayoutEngine.Contracts", "Html2x.RenderModel"]);
        Project("src", "Html2x.LayoutEngine.Pagination", "Html2x.LayoutEngine.Pagination.csproj")
            .ShouldHaveNoPackageReferences();
        Project("src", "Html2x.LayoutEngine.Contracts", "Html2x.LayoutEngine.Contracts.csproj")
            .ShouldReferenceProjects(["Html2x.RenderModel"]);
        Project("src", "Html2x.LayoutEngine.Contracts", "Html2x.LayoutEngine.Contracts.csproj")
            .ShouldHaveNoPackageReferences();
        Project("src", "Html2x.RenderModel", "Html2x.RenderModel.csproj")
            .ShouldHaveNoProjectReferences();
        Project("src", "Html2x.RenderModel", "Html2x.RenderModel.csproj")
            .ShouldHaveNoPackageReferences();
        Project("src", "Html2x.LayoutEngine.Fragments", "Html2x.LayoutEngine.Fragments.csproj")
            .ShouldReferenceProjects(["Html2x.LayoutEngine.Contracts", "Html2x.RenderModel"]);
        Project("src", "Html2x.Text", "Html2x.Text.csproj")
            .ShouldReferenceProjects(["Html2x.Diagnostics.Contracts", "Html2x.RenderModel"]);
        Project("src", "Html2x.Text", "Html2x.Text.csproj")
            .ShouldReferencePackages(["SkiaSharp", "SkiaSharp.HarfBuzz"]);
    }

    [Fact]
    public void RendererProjectGraph_StaysIndependentFromLayoutStages()
    {
        var renderer = Project("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj");

        renderer.ShouldReferenceProjects(["Html2x.Diagnostics.Contracts", "Html2x.RenderModel", "Html2x.Resources", "Html2x.Text"]);
        renderer.ShouldNotReferenceProjects(
            "Html2x.Abstractions",
            "Html2x.LayoutEngine",
            "Html2x.LayoutEngine.Contracts",
            "Html2x.LayoutEngine.Fragments",
            "Html2x.LayoutEngine.Geometry",
            "Html2x.LayoutEngine.Style");
    }

    [Fact]
    public void FocusedTestProjects_StayInOwningModules()
    {
        Project("src", "Tests", "Html2x.LayoutEngine.Pagination.Test", "Html2x.LayoutEngine.Pagination.Test.csproj")
            .ShouldReferenceProjects(["Html2x.Diagnostics.Contracts", "Html2x.LayoutEngine.Pagination", "Html2x.RenderModel"]);
        Project("src", "Tests", "Html2x.LayoutEngine.Pagination.Test", "Html2x.LayoutEngine.Pagination.Test.csproj")
            .ShouldNotReferenceProjects(
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Geometry",
                "Html2x.LayoutEngine.Style",
                "Html2x.Renderers.Pdf",
                "Html2x.Text");
        Project("src", "Tests", "Html2x.LayoutEngine.Style.Test", "Html2x.LayoutEngine.Style.Test.csproj")
            .ShouldNotReferenceProjects("Html2x.LayoutEngine", "Html2x.LayoutEngine.Geometry");
        Project("src", "Tests", "Html2x.LayoutEngine.Geometry.Test", "Html2x.LayoutEngine.Geometry.Test.csproj")
            .ShouldNotReferencePackages(ParserPackageName(), ParserPackageName() + ".Css");
    }

    [Fact]
    public void RemovedCompatibilityModules_AreAbsent()
    {
        Directory.Exists(PathFromRoot("src", "Html2x.Abstractions"))
            .ShouldBeFalse("the obsolete options-only module should be deleted.");
        Directory.Exists(PathFromRoot("src", "Html2x.LayoutEngine", "Pagination"))
            .ShouldBeFalse("pagination compatibility shims should not remain in the composition project.");
        Directory.Exists(PathFromRoot("src", "Html2x.LayoutEngine", "Fragment"))
            .ShouldBeFalse("fragment compatibility shims should not remain in the composition project.");

        ArchitectureSolution.Load("src", "Html2x.sln")
            .ProjectNames()
            .ShouldNotContain("Html2x.Abstractions");
    }
}
