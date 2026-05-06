using Html2x.RenderModel.Documents;
using Shouldly;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class ArchitectureTestHarnessTests
{
    [Theory]
    [InlineData("Children.Add")]
    [InlineData("public BlockBox")]
    public void IdentifierAssertions_CompoundPattern_Throws(string pattern)
    {
        var source = SourceFileFor<StyleNode>("Style");

        var exception = Should.Throw<ArgumentException>(() => source.ShouldNotUseIdentifier(pattern));

        exception.ParamName.ShouldBe("identifier");
    }

    [Fact]
    public void SourceSetInvocationAssertion_ReceiverMemberCall_UsesSyntax()
    {
        SourceSetFor<HtmlLayout>()
            .ShouldInvokeMemberOn("_pages", nameof(List<int>.Add));
    }
}