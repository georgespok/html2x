using Html2x.Layout.Style;

namespace Html2x.Layout.Test.Assertions;

public static class StyleAssertionExtensions
{
    public static StyleTreeAssertions AssertThat(this StyleTree tree) => new(tree);
}