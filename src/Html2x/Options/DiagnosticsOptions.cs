using Html2x.RenderModel;

namespace Html2x;


/// <summary>
/// Diagnostics options.
/// </summary>
public sealed class DiagnosticsOptions
{
    public bool EnableDiagnostics { get; init; }

    public bool IncludeRawHtml { get; init; }

    public int MaxRawHtmlLength { get; init; } = 4096;
}
