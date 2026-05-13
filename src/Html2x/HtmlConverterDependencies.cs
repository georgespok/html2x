using Html2x.Options;
using Html2x.Text;

namespace Html2x;

/// <summary>
///     Optional dependencies used by <see cref="HtmlConverter" /> for advanced in-process scenarios.
/// </summary>
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
    ///     conversion and owns the returned source for that conversion.
    /// </remarks>
    public Func<IFontSource>? FontSourceFactory { get; init; }

    /// <summary>
    ///     Gets the optional text measurer factory. The converter calls the factory once per conversion and owns the
    ///     returned measurer for that conversion.
    /// </summary>
    public Func<ITextMeasurer>? TextMeasurerFactory { get; init; }
}
