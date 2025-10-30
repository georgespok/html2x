using Html2x.Layout.Style;
using Shouldly;

namespace Html2x.Layout.Test.Assertions;

public sealed class StyleTreeAssertions(StyleTree tree)
{
    public StyleTreeAssertions HavePageMargins(float top, float right, float bottom, float left, double tolerance = 0.01)
    {
        tree.Page.MarginTopPt.ShouldBe(top, tolerance);
        tree.Page.MarginRightPt.ShouldBe(right, tolerance);
        tree.Page.MarginBottomPt.ShouldBe(bottom, tolerance);
        tree.Page.MarginLeftPt.ShouldBe(left, tolerance);
        return this;
    }

    public StyleNodeAssertions Root()
    {
        tree.Root.ShouldNotBeNull();
        return new StyleNodeAssertions(tree.Root!);
    }
}