namespace Html2x.Abstractions.Diagnostics;

public sealed class RenderSummaryPayload : IDiagnosticsPayload
{
    public string Kind => "render.summary";

    public int PageCount { get; init; }

    public int PdfSize { get; init; }
}
