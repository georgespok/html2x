using Html2x.RenderModel;

namespace Html2x;


/// <summary>
/// CSS processing options.
/// </summary>
public sealed class CssOptions
{
    /// <summary>Use the embedded default user agent stylesheet when no override is provided.</summary>
    public bool UseDefaultUserAgentStyleSheet { get; init; } = true;

    /// <summary>Overrides the embedded user agent stylesheet when set to a non-empty value.</summary>
    public string? UserAgentStyleSheet { get; init; }
}
