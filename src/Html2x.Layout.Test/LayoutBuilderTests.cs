using Html2x.Core.Layout;
using Shouldly;

namespace Html2x.Layout.Test;

public class LayoutBuilderTests
{
    [Fact]
    public async Task Build_ShouldReturnLayout()
    {
        // Arrange
        var builder = new LayoutBuilder();
        const string html = "<p>Hello, world!</p>";

        // Act
        var layout = await builder.BuildAsync(html);

        // Assert
        Assert.NotNull(layout);
        Assert.IsType<HtmlLayout>(layout);
        layout.Pages[0].Children[0].ShouldBeOfType<BlockFragment>();
        var block = (BlockFragment)layout.Pages[0].Children[0];
        block.Children[0].ShouldBeOfType<LineBoxFragment>();
        var lineBox = (LineBoxFragment)block.Children[0];
        lineBox.Runs[0].Text.ShouldBe("Hello, world!");
    }
}