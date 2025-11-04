using System.Reflection;
using AngleSharp.Dom;
using Html2x.Layout.Box;

namespace Html2x.Layout.Test.Assertions;

public static class BoxTreeAssertions
{
    public static void ShouldMatch(this BoxTree actual, BoxTree expected)
    {
        actual.ShouldMatchProperties(expected, "BoxTree");
    }

    public static void ShouldMatch(this BoxTree actual, Action<BoxTreeExpectationBuilder> configure)
    {
        var builder = new BoxTreeExpectationBuilder();
        configure(builder);
        var expected = builder.Build();
        actual.ShouldMatch(expected);
    }

    public static void ShouldMatch(this BlockBox actual, BlockBox expected)
    {
        actual.ShouldMatchProperties(expected, "BlockBox");
    }

    public static void ShouldMatch(this InlineBox actual, InlineBox expected)
    {
        actual.ShouldMatchProperties(expected, "InlineBox");
    }

    public static void ShouldMatch(this PageBox actual, PageBox expected)
    {
        actual.ShouldMatchProperties(expected, "PageBox");
    }
}

public sealed class BoxTreeExpectationBuilder
{
    private readonly BoxTree _tree = new();

    internal BoxTree Build() => _tree;

    public BoxTreeExpectationBuilder Page(Action<PageBoxExpectationBuilder> configure)
    {
        configure(new PageBoxExpectationBuilder(_tree.Page));
        return this;
    }

    public BoxTreeExpectationBuilder Block(Action<BlockExpectationBuilder> configure)
    {
        var block = new BlockBox();
        configure(new BlockExpectationBuilder(block));
        _tree.Blocks.Add(block);
        return this;
    }
}

public sealed class PageBoxExpectationBuilder
{
    private readonly PageBox _page;

    public PageBoxExpectationBuilder(PageBox page)
    {
        _page = page;
    }

    public PageBoxExpectationBuilder Margins(float top, float right, float bottom, float left)
    {
        _page.MarginTopPt = top;
        _page.MarginRightPt = right;
        _page.MarginBottomPt = bottom;
        _page.MarginLeftPt = left;
        return this;
    }
}

public sealed class BlockExpectationBuilder
{
    private readonly BlockBox _block;

    public BlockExpectationBuilder(BlockBox block)
    {
        _block = block;
    }

    public BlockExpectationBuilder Element(IElement element)
    {
        InitPropertySetter.SetElement(_block, element);
        return this;
    }

    public BlockExpectationBuilder Position(float x, float y)
    {
        _block.X = x;
        _block.Y = y;
        return this;
    }

    public BlockExpectationBuilder Width(float width)
    {
        _block.Width = width;
        return this;
    }

    public BlockExpectationBuilder Height(float height)
    {
        _block.Height = height;
        return this;
    }

    public BlockExpectationBuilder Text(string text)
    {
        var inline = new InlineBox();
        InitPropertySetter.SetText(inline, text);
        _block.Children.Add(inline);
        return this;
    }

    public BlockExpectationBuilder Inline(Action<InlineExpectationBuilder> configure)
    {
        var inline = new InlineBox();
        configure(new InlineExpectationBuilder(inline));
        _block.Children.Add(inline);
        return this;
    }

    public BlockExpectationBuilder Block(Action<BlockExpectationBuilder> configure)
    {
        var child = new BlockBox();
        configure(new BlockExpectationBuilder(child));
        _block.Children.Add(child);
        return this;
    }
}

public sealed class InlineExpectationBuilder
{
    private readonly InlineBox _inline;

    public InlineExpectationBuilder(InlineBox inline)
    {
        _inline = inline;
    }

    public InlineExpectationBuilder Element(IElement element)
    {
        InitPropertySetter.SetElement(_inline, element);
        return this;
    }

    public InlineExpectationBuilder Text(string text)
    {
        InitPropertySetter.SetText(_inline, text);
        return this;
    }

    public InlineExpectationBuilder Inline(Action<InlineExpectationBuilder> configure)
    {
        var child = new InlineBox();
        configure(new InlineExpectationBuilder(child));
        _inline.Children.Add(child);
        return this;
    }
}

internal static class InitPropertySetter
{
    private static readonly PropertyInfo ElementProperty =
        typeof(DisplayNode).GetProperty(nameof(DisplayNode.Element)) ??
        throw new InvalidOperationException("DisplayNode.Element property not found.");

    private static readonly PropertyInfo TextContentProperty =
        typeof(InlineBox).GetProperty(nameof(InlineBox.TextContent)) ??
        throw new InvalidOperationException("InlineBox.TextContent property not found.");

    public static void SetElement(DisplayNode node, IElement element)
    {
        ElementProperty.SetValue(node, element);
    }

    public static void SetText(InlineBox inline, string text)
    {
        TextContentProperty.SetValue(inline, text);
    }
}

