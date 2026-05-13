using Html2x.Options;
using Html2x.RenderModel.Measurements.Units;
using Html2x.Resources;

namespace Html2x;

internal static class HtmlConverterOptionsValidator
{
    public static void Validate(HtmlConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Page, nameof(HtmlConverterOptions.Page));
        ArgumentNullException.ThrowIfNull(options.Resources, nameof(HtmlConverterOptions.Resources));
        ArgumentNullException.ThrowIfNull(options.Css, nameof(HtmlConverterOptions.Css));
        ArgumentNullException.ThrowIfNull(options.Fonts, nameof(HtmlConverterOptions.Fonts));
        ArgumentNullException.ThrowIfNull(options.Diagnostics, nameof(HtmlConverterOptions.Diagnostics));

        ValidatePageSize(options.Page.Size);

        if (options.Resources.MaxImageSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ResourceOptions.MaxImageSizeBytes),
                "HtmlConverterOptions.Resources.MaxImageSizeBytes must be greater than zero.");
        }

        if (options.Diagnostics.MaxRawHtmlLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DiagnosticsOptions.MaxRawHtmlLength),
                "HtmlConverterOptions.Diagnostics.MaxRawHtmlLength must be greater than zero.");
        }
    }

    public static string ResolveExistingBaseDirectory(HtmlConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Resources);

        var configuredBaseDirectory = options.Resources.BaseDirectory;
        var resolvedBaseDirectory = ImageResourceLoader.ResolveBaseDirectory(configuredBaseDirectory);
        if (!string.IsNullOrWhiteSpace(configuredBaseDirectory) &&
            !Directory.Exists(resolvedBaseDirectory))
        {
            throw new DirectoryNotFoundException(
                $"HtmlConverterOptions.Resources.BaseDirectory '{configuredBaseDirectory}' does not exist.");
        }

        return resolvedBaseDirectory;
    }

    private static void ValidatePageSize(SizePt size)
    {
        if (!float.IsFinite(size.Width) ||
            !float.IsFinite(size.Height) ||
            size.Width <= 0f ||
            size.Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                "HtmlConverterOptions.Page.Size",
                size,
                "HtmlConverterOptions.Page.Size must have finite positive width and height.");
        }
    }
}
