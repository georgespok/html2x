using Html2x.RenderModel.Text;

namespace Html2x.Text;

/// <summary>
/// Represents a font resolution or font file loading failure.
/// </summary>
public sealed class FontResolutionException(
    string message,
    FontKey? requestedFont = null,
    ResolvedFont? resolvedFont = null,
    string? configuredPath = null,
    string? resolvedPath = null,
    string? text = null)
    : InvalidOperationException(message)
{
    public FontKey? RequestedFont { get; } = requestedFont;

    public ResolvedFont? ResolvedFont { get; } = resolvedFont;

    public string? ConfiguredPath { get; } = configuredPath ?? resolvedFont?.ConfiguredPath;

    public string? ResolvedPath { get; } = resolvedPath;

    public string? Text { get; } = text;
}
