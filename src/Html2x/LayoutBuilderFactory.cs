using AngleSharp;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;

namespace Html2x;

public sealed class LayoutBuilderFactory : ILayoutBuilderFactory
{
    public LayoutBuilder Create()
    {
        return Create(new LayoutServices(new FallbackTextMeasurer(), new NullFontSource()));
    }

    public LayoutBuilder Create(LayoutServices services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var angleSharpConfig = Configuration.Default.WithCss();
        var imageProvider = new FileSystemImageProvider();

        return new LayoutBuilder(
            new AngleSharpDomProvider(angleSharpConfig),
            new CssStyleComputer(new StyleTraversal(), new CssValueConverter()),
            new BoxTreeBuilder(services.TextMeasurer),
            new FragmentBuilder(),
            imageProvider,
            services.TextMeasurer,
            services.FontSource);
    }

    private sealed class FallbackTextMeasurer : ITextMeasurer
    {
        private readonly FontMetricsProvider _metricsProvider = new();

        public float MeasureWidth(FontKey font, float sizePt, string text) =>
            _metricsProvider.MeasureTextWidth(font, sizePt, text);

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) =>
            _metricsProvider.GetMetrics(font, sizePt);
    }

    private sealed class NullFontSource : IFontSource
    {
        public ResolvedFont Resolve(FontKey requested) =>
            new(requested.Family, requested.Weight, requested.Style, "fallback");
    }
}
