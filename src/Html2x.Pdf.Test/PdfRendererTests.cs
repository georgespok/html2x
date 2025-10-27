using Html2x.Core.Layout;

namespace Html2x.Pdf.Test
{
    public class PdfRendererTests
    {
        [Fact]
        public async Task RenderAsync_ShouldThrow_WhenNotImplemented()
        {
            // Arrange
            var renderer = new PdfRenderer();
            var layout = new HtmlLayout();
            var options = new PdfOptions();

            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(() => renderer.RenderAsync(layout, options));
        }
    }
}