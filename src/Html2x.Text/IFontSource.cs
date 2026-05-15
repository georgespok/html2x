using Html2x.RenderModel.Text;

namespace Html2x.Text;

/// <summary>
///     Resolves requested font facts from a configured font source.
/// </summary>
public interface IFontSource
{
    /// <summary>
    ///     Resolves the requested font for the named consumer.
    /// </summary>
    /// <param name="requested">The requested font family, weight, and style.</param>
    /// <param name="consumer">The stage or adapter requesting the font.</param>
    /// <returns>A resolved font with a non-empty source id.</returns>
    /// <remarks>
    ///     Implementations should throw <see cref="FontResolutionException" /> or another clear exception when the
    ///     requested font cannot be resolved. When the default PDF renderer will render the output, the returned
    ///     <see cref="ResolvedFont.FilePath" /> or <see cref="ResolvedFont.SourceId" /> must identify a loadable
    ///     font file.
    /// </remarks>
    ResolvedFont Resolve(FontKey requested, string consumer);
}
