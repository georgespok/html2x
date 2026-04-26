using AngleSharp.Dom;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Assertions;

internal static class BoxTreeAssertions
{
    public static void ShouldMatch(this BoxTree actual, Action<BoxTreeExpectationBuilder> configure)
    {
        var builder = new BoxTreeExpectationBuilder();
        configure(builder);

        AssertTree(actual, builder.Build());
    }

    private static void AssertTree(BoxTree actual, BoxTreeExpectation expected)
    {
        if (expected.PageMargin.HasValue)
        {
            actual.Page.Margin.ShouldBe(expected.PageMargin.Value);
        }

        if (expected.Blocks.Count == 0)
        {
            return;
        }

        actual.Blocks.Count.ShouldBe(expected.Blocks.Count);

        for (var i = 0; i < expected.Blocks.Count; i++)
        {
            AssertBlock(actual.Blocks[i], expected.Blocks[i], $"Blocks[{i}]");
        }
    }

    private static void AssertNode(DisplayNode actual, NodeExpectation expected, string path)
    {
        switch (expected)
        {
            case BlockExpectation block:
                AssertBlock(actual.ShouldBeOfType<BlockBox>($"{path} should be a block"), block, path);
                break;
            case InlineExpectation inline:
                AssertInline(actual.ShouldBeOfType<InlineBox>($"{path} should be inline"), inline, path);
                break;
            default:
                throw new InvalidOperationException($"Unsupported expectation type at {path}.");
        }
    }

    private static void AssertBlock(BlockBox actual, BlockExpectation expected, string path)
    {
        if (expected.HasElement)
        {
            actual.Element.ShouldBeSameAs(expected.Element, $"{path}.Element mismatch");
        }

        if (expected.IsAnonymous.HasValue)
        {
            actual.IsAnonymous.ShouldBe(expected.IsAnonymous.Value, $"{path}.IsAnonymous mismatch");
        }

        if (expected.Position.HasValue)
        {
            actual.X.ShouldBe(expected.Position.Value.X, $"{path}.X mismatch");
            actual.Y.ShouldBe(expected.Position.Value.Y, $"{path}.Y mismatch");
        }

        if (expected.Width.HasValue)
        {
            actual.Width.ShouldBe(expected.Width.Value, $"{path}.Width mismatch");
        }

        if (expected.Height.HasValue)
        {
            actual.Height.ShouldBe(expected.Height.Value, $"{path}.Height mismatch");
        }

        if (expected.Padding.HasValue)
        {
            actual.Padding.ShouldBe(expected.Padding.Value, $"{path}.Padding mismatch");
        }

        if (expected.Style is not null)
        {
            actual.Style.ShouldBe(expected.Style, $"{path}.Style mismatch");
        }

        AssertChildren(actual.Children, expected.Children, path);
    }

    private static void AssertInline(InlineBox actual, InlineExpectation expected, string path)
    {
        if (expected.HasElement)
        {
            actual.Element.ShouldBeSameAs(expected.Element, $"{path}.Element mismatch");
        }

        if (expected.Text is not null)
        {
            actual.TextContent.ShouldBe(expected.Text, $"{path}.TextContent mismatch");
        }

        AssertChildren(actual.Children, expected.Children, path);
    }

    private static void AssertChildren(
        IReadOnlyList<DisplayNode> actualChildren,
        IReadOnlyList<NodeExpectation> expectedChildren,
        string path)
    {
        if (expectedChildren.Count == 0)
        {
            return;
        }

        actualChildren.Count.ShouldBe(expectedChildren.Count, $"{path}.Children count mismatch");

        for (var i = 0; i < expectedChildren.Count; i++)
        {
            AssertNode(actualChildren[i], expectedChildren[i], $"{path}.Children[{i}]");
        }
    }
}

internal sealed class BoxTreeExpectationBuilder
{
    private readonly BoxTreeExpectation _tree = new();

    internal BoxTreeExpectation Build() => _tree;

    public BoxTreeExpectationBuilder Page(Action<PageBoxExpectationBuilder> configure)
    {
        configure(new PageBoxExpectationBuilder(_tree));
        return this;
    }

    public BoxTreeExpectationBuilder Block(Action<BlockExpectationBuilder> configure)
    {
        var block = new BlockExpectation();
        configure(new BlockExpectationBuilder(block));
        _tree.Blocks.Add(block);
        return this;
    }
}

internal sealed class PageBoxExpectationBuilder(BoxTreeExpectation tree)
{
    public PageBoxExpectationBuilder Margins(float top, float right, float bottom, float left)
    {
        tree.PageMargin = new Spacing(top, right, bottom, left);
        return this;
    }
}

internal sealed class BlockExpectationBuilder(BlockExpectation block)
{
    public BlockExpectationBuilder IsAnonymous(bool value)
    {
        block.IsAnonymous = value;
        return this;
    }

    public BlockExpectationBuilder Element(IElement element)
    {
        block.Element = element;
        block.HasElement = true;
        return this;
    }

    public BlockExpectationBuilder Position(float x, float y)
    {
        block.Position = new ExpectedPoint(x, y);
        return this;
    }

    public BlockExpectationBuilder Width(float width)
    {
        block.Width = width;
        return this;
    }

    public BlockExpectationBuilder Height(float height)
    {
        block.Height = height;
        return this;
    }

    public BlockExpectationBuilder Text(string text)
    {
        block.Children.Add(new InlineExpectation { Text = text });
        return this;
    }

    public BlockExpectationBuilder Style(ComputedStyle style)
    {
        block.Style = style;
        return this;
    }

    public BlockExpectationBuilder Inline(Action<InlineExpectationBuilder> configure)
    {
        var inline = new InlineExpectation();
        configure(new InlineExpectationBuilder(inline));
        block.Children.Add(inline);
        return this;
    }

    public BlockExpectationBuilder Block(Action<BlockExpectationBuilder> configure)
    {
        var child = new BlockExpectation();
        configure(new BlockExpectationBuilder(child));
        block.Children.Add(child);
        return this;
    }

    public BlockExpectationBuilder Padding(float top, float right, float bottom, float left)
    {
        block.Padding = new Spacing(top, right, bottom, left);
        return this;
    }
}

internal sealed class InlineExpectationBuilder(InlineExpectation inline)
{
    public InlineExpectationBuilder Element(IElement element)
    {
        inline.Element = element;
        inline.HasElement = true;
        return this;
    }

    public InlineExpectationBuilder Text(string text)
    {
        if (inline.HasElement)
        {
            inline.Children.Add(new InlineExpectation { Text = text });
            return this;
        }

        inline.Text = text;
        return this;
    }

    public InlineExpectationBuilder Inline(Action<InlineExpectationBuilder> configure)
    {
        var child = new InlineExpectation();
        configure(new InlineExpectationBuilder(child));
        inline.Children.Add(child);
        return this;
    }
}

internal sealed class BoxTreeExpectation
{
    public Spacing? PageMargin { get; set; }

    public List<BlockExpectation> Blocks { get; } = [];
}

internal abstract class NodeExpectation
{
    public List<NodeExpectation> Children { get; } = [];
}

internal sealed class BlockExpectation : NodeExpectation
{
    public bool HasElement { get; set; }

    public IElement? Element { get; set; }

    public bool? IsAnonymous { get; set; }

    public ExpectedPoint? Position { get; set; }

    public float? Width { get; set; }

    public float? Height { get; set; }

    public Spacing? Padding { get; set; }

    public ComputedStyle? Style { get; set; }
}

internal sealed class InlineExpectation : NodeExpectation
{
    public bool HasElement { get; set; }

    public IElement? Element { get; set; }

    public string? Text { get; set; }
}

internal readonly record struct ExpectedPoint(float X, float Y);
