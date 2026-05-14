using Html2x.LayoutEngine;
using Html2x.Options;
using Html2x.Renderers.Pdf;
using Html2x.Resources;

namespace Html2x;

internal static class HtmlConverterSettingsMapper
{
    public static LayoutBuildSettings ToLayoutBuildSettings(HtmlConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new()
        {
            PageSize = options.Page.Size,
            Style = new()
            {
                UseDefaultUserAgentStyleSheet = options.Css.UseDefaultUserAgentStyleSheet,
                UserAgentStyleSheet = options.Css.UserAgentStyleSheet
            }
        };
    }

    public static PdfRenderSettings ToPdfRenderSettings(
        HtmlConverterOptions options,
        string baseDirectory,
        IImageResourceReader imageResources)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(imageResources);

        return new()
        {
            ResourceBaseDirectory = baseDirectory,
            MaxImageSizeBytes = options.Resources.MaxImageSizeBytes,
            IncludeRawImageSources = options.Diagnostics.IncludeRawHtml,
            MaxRawImageSourceLength = options.Diagnostics.MaxRawHtmlLength,
            ImageResources = imageResources
        };
    }
}
