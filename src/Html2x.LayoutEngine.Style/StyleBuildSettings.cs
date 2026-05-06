namespace Html2x.LayoutEngine.Style;

/// <summary>
///     Input settings owned by the style stage.
/// </summary>
internal sealed class StyleBuildSettings
{
    /// <summary>Use the embedded default user agent stylesheet when no override is provided.</summary>
    public bool UseDefaultUserAgentStyleSheet { get; init; } = true;

    /// <summary>Overrides the embedded user agent stylesheet when set to a non-empty value.</summary>
    public string? UserAgentStyleSheet { get; init; }
}