using Html2x.RenderModel;

namespace Html2x.Text;

/// <summary>
/// Represents a font resolution or font file loading failure.
/// </summary>
public sealed class FontResolutionException : InvalidOperationException
{
    public FontResolutionException(
        string message,
        FontKey? requestedFont = null,
        ResolvedFont? resolvedFont = null,
        string? configuredPath = null,
        string? resolvedPath = null,
        string? text = null)
        : base(message)
    {
        RequestedFont = requestedFont;
        ResolvedFont = resolvedFont;
        ConfiguredPath = configuredPath ?? resolvedFont?.ConfiguredPath;
        ResolvedPath = resolvedPath;
        Text = text;
    }

    public FontKey? RequestedFont { get; }

    public ResolvedFont? ResolvedFont { get; }

    public string? ConfiguredPath { get; }

    public string? ResolvedPath { get; }

    public string? Text { get; }
}
