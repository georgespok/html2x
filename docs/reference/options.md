# Options

This page describes public converter option groups, validation, defaults, and
how options map into internal stage settings.

## `HtmlConverterOptions`

`HtmlConverterOptions` groups:

- `Page`: page-level conversion options such as page size.
- `Resources`: resource loading options such as base directory and image size
  limit.
- `Css`: CSS processing options such as user agent stylesheet behavior.
- `Fonts`: font resolution options.
- `Diagnostics`: diagnostics enablement and raw HTML capture options.

## Example

```csharp
using Html2x;
using Html2x.RenderModel;

var options = new HtmlConverterOptions
{
    Page = new PageOptions
    {
        Size = PaperSizes.A4
    },
    Resources = new ResourceOptions
    {
        BaseDirectory = resourceBaseDirectory,
        MaxImageSizeBytes = 10 * 1024 * 1024
    },
    Css = new CssOptions
    {
        UseDefaultUserAgentStyleSheet = true
    },
    Fonts = new FontOptions
    {
        FontPath = fontPath
    },
    Diagnostics = new DiagnosticsOptions
    {
        EnableDiagnostics = true
    }
};
```

## Page Options

`PageOptions.Size` controls the page size used by layout, pagination, and PDF
rendering. Paper size values are render model facts under `Html2x.RenderModel`.

## Resource Options

`ResourceOptions.BaseDirectory` controls relative image path scope. If it is
not set, the converter uses `AppContext.BaseDirectory`. Set it explicitly when
HTML references relative image paths.

`ResourceOptions.MaxImageSizeBytes` controls image metadata checks and render
byte loading. Oversized images produce deterministic status and diagnostics.

## CSS Options

`CssOptions.UseDefaultUserAgentStyleSheet` controls whether Html2x applies its
default stylesheet before authored CSS.

## Font Options

With the default converter runtime, `FontOptions.FontPath` is required. It must
point to an existing font file or directory before layout begins.

The converter maps this path into `FontPathSource`,
`DiagnosticsFontSource` when enabled, and `SkiaTextMeasurer` during layout.

When a caller constructs `HtmlConverter` with `HtmlConverterRuntime.FontSource`,
the converter uses that source instead of `FontOptions.FontPath`. When a caller
supplies `HtmlConverterRuntime.TextMeasurer`, the converter uses that measurer
directly and the caller owns its lifetime.

## Diagnostics Options

`DiagnosticsOptions.EnableDiagnostics` enables collection of diagnostic
records. `DiagnosticsOptions.IncludeRawHtml` controls raw HTML capture, and
`DiagnosticsOptions.MaxRawHtmlLength` caps captured payload length.

Raw HTML is omitted by default.

## Internal Mapping

`Html2x` maps public options into stage-owned settings:

- `StyleBuildSettings`
- `LayoutBuildSettings`
- `LayoutGeometryRequest`
- `PaginationOptions`
- `PdfRenderSettings`

Internal stages consume those settings instead of public option objects.
