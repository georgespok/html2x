using Shouldly;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class BoxNodeChildMutationTests
{
    public static IEnumerable<object[]> MismatchedParentCases()
    {
        yield return
        [
            "AddChild",
            (Action<BlockBox, BoxNode>)((parent, child) => parent.AddChild(child)),
            "child"
        ];
        yield return
        [
            "InsertChild",
            (Action<BlockBox, BoxNode>)((parent, child) => parent.InsertChild(0, child)),
            "child"
        ];
        yield return
        [
            "AddChildren",
            (Action<BlockBox, BoxNode>)((parent, child) => parent.AddChildren([child])),
            "children"
        ];
        yield return
        [
            "ReplaceChildren",
            (Action<BlockBox, BoxNode>)((parent, child) => parent.ReplaceChildren([child])),
            "children"
        ];
    }

    [Theory]
    [MemberData(nameof(MismatchedParentCases))]
    public void ChildMutation_MismatchedParent_ThrowsWithoutAdding(
        string caseName,
        object attachChildAction,
        string expectedParameterName)
    {
        _ = caseName;
        var parent = new BlockBox(BoxRole.Block);
        var otherParent = new BlockBox(BoxRole.Block);
        var child = new BlockBox(BoxRole.Block)
        {
            Parent = otherParent
        };
        var attachChild = attachChildAction.ShouldBeOfType<Action<BlockBox, BoxNode>>();

        var exception = Should.Throw<ArgumentException>(() => attachChild(parent, child));

        exception.ParamName.ShouldBe(expectedParameterName);
        parent.Children.ShouldBeEmpty();
    }

    [Fact]
    public void AddChild_NullParent_AllowsStandaloneChild()
    {
        var parent = new BlockBox(BoxRole.Block);
        var child = new InlineBox(BoxRole.Inline);

        parent.AddChild(child);

        parent.Children.ShouldBe([child]);
        child.Parent.ShouldBeNull();
    }

    [Fact]
    public void AddChild_SameParent_AllowsChild()
    {
        var parent = new BlockBox(BoxRole.Block);
        var child = new InlineBox(BoxRole.Inline)
        {
            Parent = parent
        };

        parent.AddChild(child);

        parent.Children.ShouldBe([child]);
    }

    [Fact]
    public void AddChildren_InvalidLaterChild_DoesNotPartiallyMutate()
    {
        var parent = new BlockBox(BoxRole.Block);
        var otherParent = new BlockBox(BoxRole.Block);
        var validChild = new BlockBox(BoxRole.Block)
        {
            Parent = parent
        };
        var invalidChild = new BlockBox(BoxRole.Block)
        {
            Parent = otherParent
        };

        var exception = Should.Throw<ArgumentException>(() =>
            parent.AddChildren([validChild, invalidChild]));

        exception.ParamName.ShouldBe("children");
        parent.Children.ShouldBeEmpty();
    }

    [Fact]
    public void ReplaceChildren_InvalidLaterChild_PreservesExistingChildren()
    {
        var parent = new BlockBox(BoxRole.Block);
        var otherParent = new BlockBox(BoxRole.Block);
        var existingChild = new BlockBox(BoxRole.Block)
        {
            Parent = parent
        };
        var validReplacement = new BlockBox(BoxRole.Block)
        {
            Parent = parent
        };
        var invalidReplacement = new BlockBox(BoxRole.Block)
        {
            Parent = otherParent
        };
        parent.AddChild(existingChild);

        var exception = Should.Throw<ArgumentException>(() =>
            parent.ReplaceChildren([validReplacement, invalidReplacement]));

        exception.ParamName.ShouldBe("children");
        parent.Children.ShouldBe([existingChild]);
    }
}
