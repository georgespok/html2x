using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine;
using Html2x.Renderers.Pdf.Options;
using Html2x.Renderers.Pdf.Pipeline;

namespace Html2x;

public class HtmlConverter : IDiagnosticsHost
{
    private readonly ILayoutBuilderFactory _layoutBuilderFactory;
    private readonly Func<LayoutBuilder, LayoutBuilder> _layoutDecorator;
    private readonly Func<PdfRenderer, PdfRenderer> _rendererDecorator;
    private readonly Func<IDiagnosticSession?, PdfRenderer> _rendererFactory;
    private Func<IDiagnosticSession?>? _diagnosticSessionAccessor;

    public HtmlConverter(
        ILayoutBuilderFactory? layoutBuilderFactory = null,
        Func<LayoutBuilder, LayoutBuilder>? layoutDecorator = null,
        Func<PdfRenderer, PdfRenderer>? rendererDecorator = null)
    {
        _layoutBuilderFactory = layoutBuilderFactory ?? new LayoutBuilderFactory();
        _rendererFactory = session => new PdfRenderer(session);

        _layoutDecorator = layoutDecorator ?? (builder => builder);
        _rendererDecorator = rendererDecorator ?? (renderer => renderer);
    }

    public async Task<byte[]> ToPdfAsync(string html, PdfOptions options)
    {
        var session = _diagnosticSessionAccessor?.Invoke();
        var layoutBuilder = _layoutDecorator(_layoutBuilderFactory.Create(session))
                            ?? throw new InvalidOperationException("Layout decorator returned null.");

        var layout = await layoutBuilder.BuildAsync(html, options.PageSize);

        var renderer = _rendererDecorator(_rendererFactory(session))
                       ?? throw new InvalidOperationException("Renderer decorator returned null.");
        return await renderer.RenderAsync(layout, options);
    }

    public void AttachDiagnosticsSession(Func<IDiagnosticSession?> accessor)
    {
        _diagnosticSessionAccessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }
}



