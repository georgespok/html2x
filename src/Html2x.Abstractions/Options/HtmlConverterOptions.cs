namespace Html2x.Abstractions.Options;

public sealed class HtmlConverterOptions
{
    public LayoutOptions Layout { get; init; } = new();

    public PdfOptions Pdf { get; init; } = new();

    public DiagnosticsOptions Diagnostics { get; init; } = new();
}
