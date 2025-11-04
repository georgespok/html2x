using Html2x.Layout;
using Html2x.Pdf;

namespace Html2x;

public class HtmlConverter(ILayoutBuilderFactory? layoutBuilderFactory = null)
{
    private readonly LayoutBuilder _layoutBuilder = (layoutBuilderFactory ?? new LayoutBuilderFactory()).Create();

    public async Task<byte[]> ToPdfAsync(string html, PdfOptions options)
    {
        var layout = await _layoutBuilder.BuildAsync(html, options.PageSize);

        var renderer = new PdfRenderer();
        return await renderer.RenderAsync(layout, options);
    }
}