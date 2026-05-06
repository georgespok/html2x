using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Measurements.Units;
using Html2x.Text;
using Html2x.LayoutEngine.Test.TestDoubles;

namespace Html2x.LayoutEngine.Test;

internal sealed class LayoutBuilderFixture
{
    internal Task<HtmlLayout> BuildLayoutAsync(
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