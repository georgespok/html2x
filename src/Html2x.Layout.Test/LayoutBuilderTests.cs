using Html2x.Core.Layout;

namespace Html2x.Layout.Test
{
    public class LayoutBuilderTests
    {
        [Fact]
        public void Build_ShouldReturnLayout()
        {
            // Arrange
            var builder = new LayoutBuilder();
            var html = "<p>Hello, world!</p>";

            // Act
            var layout = builder.Build(html);

            // Assert
            Assert.NotNull(layout);
            Assert.IsType<HtmlLayout>(layout);
        }
    }
}