namespace Html2x.Abstractions.Options;

public sealed class HtmlConverterOptions
{
    public LayoutOptions Layout { get; set; } = new();

    public PdfOptions Pdf { get; set; } = new();

    public DiagnosticsOptions Diagnostics { get; set; } = new();
}
