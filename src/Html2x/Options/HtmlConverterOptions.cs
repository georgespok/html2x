namespace Html2x.Options;


/// <summary>
/// Public conversion request options for HTML to PDF conversion.
/// </summary>
public sealed class HtmlConverterOptions
{
    public PageOptions Page { get; init; } = new();

    public ResourceOptions Resources { get; init; } = new();

    public CssOptions Css { get; init; } = new();

    public FontOptions Fonts { get; init; } = new();

    public DiagnosticsOptions Diagnostics { get; init; } = new();
}
