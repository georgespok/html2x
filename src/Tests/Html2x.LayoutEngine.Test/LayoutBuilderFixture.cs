using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;

namespace Html2x.LayoutEngine.Test;

public sealed class LayoutBuilderFixture
{
    public Task<HtmlLayout> BuildLayoutAsync(
        string html,
        ITextMeasurer textMeasurer,
        LayoutOptions? options = null,
        IImageProvider? imageProvider = null,
        IFontSource? fontSource = null)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException("HTML must be provided.", nameof(html));
        }

        ArgumentNullException.ThrowIfNull(textMeasurer);

        var services = new LayoutServices(
            textMeasurer,
            fontSource ?? CreateFontSource(),
            imageProvider ?? new NoopImageProvider());

        var layoutBuilder = new LayoutBuilderFactory().Create(services);
        var layoutOptions = options ?? new LayoutOptions { PageSize = PaperSizes.A4 };

        return layoutBuilder.BuildAsync(html, layoutOptions);
    }

    public static IFontSource CreateFontSource()
    {
        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));
        return fontSource.Object;
    }
}
