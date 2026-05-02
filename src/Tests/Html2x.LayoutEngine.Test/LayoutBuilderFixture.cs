using Html2x.LayoutEngine.Geometry.Images;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test;

public sealed class LayoutBuilderFixture
{
    public Task<HtmlLayout> BuildLayoutAsync(
        string html,
        ITextMeasurer textMeasurer,
        LayoutBuildSettings? options = null,
        IImageMetadataResolver? imageMetadataResolver = null)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException("HTML must be provided.", nameof(html));
        }

        ArgumentNullException.ThrowIfNull(textMeasurer);

        var layoutBuilder = new LayoutBuilder(
            textMeasurer,
            imageMetadataResolver ?? new NoopImageMetadataResolver());
        var layoutOptions = options ?? new LayoutBuildSettings { PageSize = PaperSizes.A4 };

        return layoutBuilder.BuildAsync(html, layoutOptions);
    }
}
