namespace Html2x.Options;

/// <summary>
///     Diagnostics options.
/// </summary>
public sealed class DiagnosticsOptions
{
    public bool EnableDiagnostics { get; init; }

    public bool IncludeRawHtml { get; init; }

    public int MaxRawHtmlLength { get; init; } = 4096;
}