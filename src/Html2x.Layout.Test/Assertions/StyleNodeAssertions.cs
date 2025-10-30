using Html2x.Layout.Style;
using Shouldly;

namespace Html2x.Layout.Test.Assertions;

public sealed class StyleNodeAssertions(StyleNode node)
{
    public StyleNodeAssertions HasTag(string tag)
    {
        node.Element.TagName.ShouldBe(tag);
        return this;
    }

    public StyleNodeAssertions HasChildrenCount(int count)
    {
        node.Children.Count.ShouldBe(count);
        return this;
    }

    public StyleNodeAssertions Child(int index)
    {
        index.ShouldBeGreaterThanOrEqualTo(0);
        index.ShouldBeLessThan(node.Children.Count);
        return new StyleNodeAssertions(node.Children[index]);
    }

    public StyleNodeAssertions Child(string tag, int occurrence = 0)
    {
        tag.ShouldNotBeNull();
        var matches = node.Children
            .Where(c => string.Equals(c.Element.TagName, tag, StringComparison.OrdinalIgnoreCase))
            .ToList();
        occurrence.ShouldBeGreaterThanOrEqualTo(0);
        occurrence.ShouldBeLessThan(matches.Count);
        return new StyleNodeAssertions(matches[occurrence]);
    }

    public StyleNodeAssertions WithChild(string tag, Action<StyleNodeAssertions> action, int occurrence = 0)
    {
        var child = Child(tag, occurrence);
        action(child);
        return this;
    }

    public StyleNodeAssertions Style(Action<ComputedStyleAssertions> assert)
    {
        var a = new ComputedStyleAssertions(node.Style);
        assert(a);
        return this;
    }
}