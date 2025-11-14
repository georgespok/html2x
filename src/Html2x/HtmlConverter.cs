using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Html2x.Renderers.Pdf.Options;
using Html2x.Renderers.Pdf.Pipeline;

namespace Html2x;

public class HtmlConverter : IDiagnosticsHost
{
    private readonly ILayoutBuilderFactory _layoutBuilderFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HtmlConverter> _logger;
    private readonly Func<LayoutBuilder, LayoutBuilder> _layoutDecorator;
    private readonly Func<PdfRenderer, PdfRenderer> _rendererDecorator;
    private Func<IDiagnosticSession?>? _diagnosticSessionAccessor;

    public HtmlConverter(
        ILayoutBuilderFactory? layoutBuilderFactory = null,
        ILoggerFactory? loggerFactory = null,
        Func<LayoutBuilder, LayoutBuilder>? layoutDecorator = null,
        Func<PdfRenderer, PdfRenderer>? rendererDecorator = null)
    {
        _layoutBuilderFactory = layoutBuilderFactory ?? new LayoutBuilderFactory();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<HtmlConverter>();

        _layoutDecorator = layoutDecorator ?? (builder => builder);
        _rendererDecorator = rendererDecorator ?? (renderer => renderer);
    }

    public async Task<byte[]> ToPdfAsync(string html, PdfOptions options)
    {
        _logger.LogDebug("Converting HTML to PDF with target page size {Width}x{Height}", options.PageSize.Width, options.PageSize.Height);

        var session = _diagnosticSessionAccessor?.Invoke();
        var layoutBuilder = _layoutDecorator(_layoutBuilderFactory.Create(_loggerFactory, session))
                            ?? throw new InvalidOperationException("Layout decorator returned null.");

        var layout = await layoutBuilder.BuildAsync(html, options.PageSize);

        var renderer = _rendererDecorator(new PdfRenderer(_loggerFactory, session))
                       ?? throw new InvalidOperationException("Renderer decorator returned null.");
        return await renderer.RenderAsync(layout, options);
    }

    public void AttachDiagnosticsSession(Func<IDiagnosticSession?> accessor)
    {
        _diagnosticSessionAccessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }
}



