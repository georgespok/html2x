using Html2x.Options;
using Html2x.Resources;
using Html2x.Text;

namespace Html2x;

internal sealed class ConversionResources : IDisposable
{
    private readonly IDisposable? _ownedFontSource;
    private readonly IDisposable? _ownedTextMeasurer;

    private ConversionResources(
        ITextMeasurer textMeasurer,
        IDisposable? ownedTextMeasurer,
        IDisposable? ownedFontSource,
        ImageResourceStore imageResources)
    {
        TextMeasurer = textMeasurer;
        _ownedTextMeasurer = ownedTextMeasurer;
        _ownedFontSource = ownedFontSource;
        ImageResources = imageResources;
        ImageMetadataResolver = new(imageResources);
    }

    public ITextMeasurer TextMeasurer { get; }

    public ImageResourceStore ImageResources { get; }

    public ImageResourceMetadataResolver ImageMetadataResolver { get; }

    public static ConversionResources Create(
        HtmlConverterDependencies dependencies,
        HtmlConverterOptions options,
        string baseDirectory,
        HtmlConversionDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var ownedFontSource = default(IDisposable);
        var textMeasurer = CreateTextMeasurer(dependencies, options, diagnostics, out ownedFontSource);
        var ownedTextMeasurer = textMeasurer as IDisposable;

        var imageResources = new ImageResourceStore(
            baseDirectory,
            options.Resources.MaxImageSizeBytes);

        return new(textMeasurer, ownedTextMeasurer, ownedFontSource, imageResources);
    }

    public void Dispose()
    {
        _ownedTextMeasurer?.Dispose();
        _ownedFontSource?.Dispose();
    }

    private static ITextMeasurer CreateTextMeasurer(
        HtmlConverterDependencies dependencies,
        HtmlConverterOptions options,
        HtmlConversionDiagnostics diagnostics,
        out IDisposable? ownedFontSource)
    {
        ownedFontSource = null;
        if (dependencies.TextMeasurerFactory is { } createTextMeasurer)
        {
            return createTextMeasurer()
                   ?? throw new InvalidOperationException(
                       "HtmlConverterDependencies.TextMeasurerFactory returned null.");
        }

        var fontSource = ResolveFontSource(dependencies, options, diagnostics, out ownedFontSource);
        if (diagnostics.Sink is not null)
        {
            fontSource = new DiagnosticsFontSource(fontSource, diagnostics.Sink);
        }

        return new SkiaTextMeasurer(fontSource);
    }

    private static IFontSource ResolveFontSource(
        HtmlConverterDependencies dependencies,
        HtmlConverterOptions options,
        HtmlConversionDiagnostics diagnostics,
        out IDisposable? ownedFontSource)
    {
        ownedFontSource = null;
        if (dependencies.FontSourceFactory is { } createFontSource)
        {
            var dependencyFontSource = createFontSource()
                                       ?? throw new InvalidOperationException(
                                           "HtmlConverterDependencies.FontSourceFactory returned null.");
            ownedFontSource = dependencyFontSource as IDisposable;
            return dependencyFontSource;
        }

        var fontPath = options.Fonts.FontPath;
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw diagnostics.CreateFontPathException(
                "HtmlConverterOptions.Fonts.FontPath must be provided before layout can begin.");
        }

        try
        {
            return new FontPathSource(fontPath);
        }
        catch (FontResolutionException)
        {
            throw diagnostics.CreateFontPathException(
                $"HtmlConverterOptions.Fonts.FontPath '{fontPath}' does not exist.");
        }
    }
}
