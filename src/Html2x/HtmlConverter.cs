using Html2x.LayoutEngine;
using Html2x.Pdf.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Html2x.Abstractions.Measurements.Units;

using Html2x.Pdf.Options;
using Html2x.Renderers.Pdf.Pipeline;

namespace Html2x;

public class HtmlConverter
{
    private readonly LayoutBuilder _layoutBuilder;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HtmlConverter> _logger;

    public HtmlConverter(
        ILayoutBuilderFactory? layoutBuilderFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<HtmlConverter>();

        var effectiveFactory = layoutBuilderFactory ?? new LayoutBuilderFactory();
        _layoutBuilder = effectiveFactory.Create(_loggerFactory);
    }

    public async Task<byte[]> ToPdfAsync(string html, PdfOptions options)
    {
        _logger.LogDebug("Converting HTML to PDF with target page size {Width}x{Height}", options.PageSize.Width, options.PageSize.Height);

        var layout = await _layoutBuilder.BuildAsync(html, options.PageSize);

        var renderer = new PdfRenderer(_loggerFactory);
        return await renderer.RenderAsync(layout, options);
    }
}



