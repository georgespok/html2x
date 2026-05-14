using Html2x.LayoutEngine.Fragments;
using Html2x.RenderModel.Documents;
using Shouldly;
using Html2x.Architecture.Test.Support;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test;

public sealed class TestHarnessTests
{
    [Theory]
    [InlineData("Children.Add")]
    [InlineData("public BlockBox")]
    public void IdentifierAssertions_CompoundPattern_Throws(string pattern)
    {
        var source = SourceFileFor<StyleNode>();

        var exception = Should.Throw<ArgumentException>(() => source.ShouldNotUseIdentifier(pattern));

        exception.ParamName.ShouldBe("identifier");
    }

    [Fact]
    public void SourceSetInvocationAssertion_ReceiverMemberCall_UsesSyntax()
    {
        SourceSetFor<HtmlLayout>()
            .ShouldInvokeMemberOn("_pages", nameof(List<int>.Add));
    }

    [Fact]
    public void TypeAnchoredSourceLookup_FindsSourceFileWithoutFolderString()
    {
        var source = SourceFileFor<HtmlLayout>();

        source.RequiredFullTypeName().ShouldBe(FullTypeName<HtmlLayout>());
        source.Path.ShouldEndWith(Path.Combine("Documents", nameof(HtmlLayout) + RepositoryLayout.CSharpFileExtension));
    }

    [Fact]
    public void TypeAnchoredProjectLookup_UsesAssemblyAnchor()
    {
        ProjectFor(typeof(HtmlLayout))
            .TargetFrameworks()
            .ShouldContain(RepositoryLayout.Net8TargetFramework);
    }

    [Fact]
    public void SemanticProject_TypeAnchor_FindsSymbolSource()
    {
        var source = SemanticProjectFor<HtmlLayout>().SourceFileForType<HtmlLayout>();

        source.RequiredFullTypeName().ShouldBe(FullTypeName<HtmlLayout>());
    }

    [Fact]
    public void WorkspaceProject_CurrentProject_LoadsMetadata()
    {
        var project = CurrentWorkspaceProject();

        project.ShouldHaveAssemblyName(CurrentAssemblyName());
        project.SourceFilePaths
            .Any(path => path.EndsWith(nameof(TestSupport) + ".cs", StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    [Fact]
    public void CompiledTypeAssertions_UseReflectionMetadata()
    {
        var type = CompiledType.For<HtmlLayout>();

        type.ShouldHaveAccessibility(InternalAccessibility);
        type.ShouldContainProperty(nameof(HtmlLayout.Pages), typeof(IReadOnlyList<LayoutPage>), PublicAccessibility);
    }

    [Fact]
    public void CompiledAssemblyAssertions_ReadFriendAssemblies()
    {
        CompiledAssembly.For<HtmlLayout>()
            .ShouldContainFriendAssemblies(CurrentAssemblyName());
    }

    [Fact]
    public void MarkdownTopicAssertions_UseDocumentSectionAndTopicAnchors()
    {
        DocumentAnchors.ArchitecturePipeline.Composition
            .ShouldMentionTopics(
                DocumentTopic.AssemblyOf<StyleNode>(),
                DocumentTopic.AssemblyOf<FragmentTreeBuilder>());
    }

    [Fact]
    public void LiteralExceptionLedger_RequiredFields_AreExplicit()
    {
        LiteralExceptionLedger.RequiredFields.ShouldBeSet(
        [
            LiteralExceptionLedger.LiteralField,
            LiteralExceptionLedger.CategoryField,
            LiteralExceptionLedger.ReasonField,
            LiteralExceptionLedger.FutureCleanupOptionField,
            LiteralExceptionLedger.ReviewOutcomeField
        ]);
    }
}
