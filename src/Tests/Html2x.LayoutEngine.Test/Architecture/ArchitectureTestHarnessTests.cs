using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class ArchitectureTestHarnessTests
{
    [Theory]
    [InlineData("Children.Add")]
    [InlineData("public BlockBox")]
    public void IdentifierAssertions_CompoundPattern_Throws(string pattern)
    {
        var source = CSharpSourceFile.Load(
            "src",
            "Html2x.LayoutEngine.Contracts",
            "Style",
            "StyleNode.cs");

        var exception = Should.Throw<ArgumentException>(() => source.ShouldNotUseIdentifier(pattern));

        exception.ParamName.ShouldBe("identifier");
    }

    [Fact]
    public void SourceSetInvocationAssertion_ReceiverMemberCall_UsesSyntax()
    {
        CSharpSourceSet.FromDirectory("src", "Html2x.RenderModel")
            .ShouldInvokeMemberOn("_pages", "Add");
    }
}
