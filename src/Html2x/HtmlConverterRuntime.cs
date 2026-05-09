using Html2x.Options;
using Html2x.Text;

namespace Html2x;

/// <summary>
///     Optional runtime adapters used by <see cref="HtmlConverter" /> for advanced in-process scenarios.
/// </summary>
public sealed class HtmlConverterRuntime
{
    /// <summary>
    ///     Gets the default runtime adapter set. Default conversion resolves fonts from
    ///     <see cref="FontOptions.FontPath" /> and uses the built-in Skia text measurer.
    /// </summary>
    public static HtmlConverterRuntime Default { get; } = new();

    /// <summary>
    ///     Gets the optional font source used when <see cref="TextMeasurer" /> is not supplied.
    /// </summary>
    /// <remarks>
    ///     Supplying a font source lets callers provide fonts from an approved in-process source
    ///     without requiring <see cref="FontOptions.FontPath" />. The caller owns the font
    ///     source lifetime.
    /// </remarks>
    public IFontSource? FontSource { get; init; }

    /// <summary>
    ///     Gets the optional text measurer. When supplied, the caller owns the measurer lifetime.
    /// </summary>
    public ITextMeasurer? TextMeasurer { get; init; }
}
