namespace Html2x.RenderModel.Text;

/// <summary>
///     Describes the font face selected during measurement without exposing renderer-specific types.
/// </summary>
/// <param name="Family">The resolved font family.</param>
/// <param name="Weight">The resolved font weight.</param>
/// <param name="Style">The resolved font style.</param>
/// <param name="SourceId">Stable non-empty identity for the resolved source.</param>
/// <param name="FilePath">Optional font file path used by renderers that load local font files.</param>
/// <param name="FaceIndex">Face index inside a font collection.</param>
/// <param name="ConfiguredPath">The caller-configured font path or source label when available.</param>
/// <remarks>
///     Layout requires a non-empty <paramref name="SourceId" />. The default PDF renderer also needs
///     <paramref name="FilePath" /> or <paramref name="SourceId" /> to identify a loadable font file.
/// </remarks>
public sealed record ResolvedFont(
    string Family,
    FontWeight Weight,
    FontStyle Style,
    string SourceId,
    string? FilePath = null,
    int FaceIndex = 0,
    string? ConfiguredPath = null);
