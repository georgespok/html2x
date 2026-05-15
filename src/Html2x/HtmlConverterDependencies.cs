using Html2x.Options;
using Html2x.Text;

namespace Html2x;

/// <summary>
///     Optional dependencies used by <see cref="HtmlConverter" /> for advanced in-process scenarios.
/// </summary>
/// <remarks>
///     Dependencies are conversion-scoped adapters, not a service container. The converter calls the chosen factory
///     once per conversion and disposes the returned adapter when it implements <see cref="IDisposable" />.
/// </remarks>
public sealed class HtmlConverterDependencies
{
    /// <summary>
    ///     Gets the default converter dependencies. Default conversion resolves fonts from
    ///     <see cref="FontOptions.FontPath" /> and uses the built-in Skia text measurer.
    /// </summary>
    public static HtmlConverterDependencies Default { get; } = new();

    /// <summary>
    ///     Gets the optional font source factory used when <see cref="TextMeasurerFactory" /> is not supplied.
    /// </summary>
    /// <remarks>
    ///     Supplying a font source factory lets callers provide fonts from an approved in-process source
    ///     without requiring <see cref="FontOptions.FontPath" />. The converter calls the factory once per
    ///     conversion and owns the returned source for that conversion. This factory is ignored when
    ///     <see cref="TextMeasurerFactory" /> is supplied.
    /// </remarks>
    public Func<IFontSource>? FontSourceFactory { get; init; }

    /// <summary>
    ///     Gets the optional text measurer factory used directly by layout measurement.
    /// </summary>
    /// <remarks>
    ///     A custom measurer must return complete <see cref="TextMeasurement" /> facts for every call. Those facts
    ///     must include finite non-negative width, ascent, and descent values plus a resolved font with a non-empty
    ///     source id. Renderer font file loadability is validated later by the PDF renderer.
    /// </remarks>
    public Func<ITextMeasurer>? TextMeasurerFactory { get; init; }
}
