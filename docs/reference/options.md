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
using Html2x.Options;
using Html2x.RenderModel.Measurements.Units;

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
rendering. Paper size values are public unit facts under
`Html2x.RenderModel.Measurements.Units`.

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

With the default converter dependencies, `FontOptions.FontPath` is required. It
must point to an existing font file or directory before layout begins.

The converter maps this path into `FontPathSource`, an internal diagnostics
font wrapper when diagnostics are enabled, and `SkiaTextMeasurer` during
layout.

When a caller constructs `HtmlConverter` with
`HtmlConverterDependencies.FontSourceFactory`, the converter uses the
factory-created source instead of `FontOptions.FontPath`. When a caller
supplies `HtmlConverterDependencies.TextMeasurerFactory`, the converter uses
the factory-created measurer directly. Dependency factories are called once per
conversion, and the converter disposes returned adapters that implement
`IDisposable`. A custom text measurer implements a single always-complete
`Measure` method and must return finite non-negative width, ascent, and descent
values plus a resolved font with a non-empty source id.
Factories must return non-null adapters. A null return or thrown exception fails
as converter configuration before layout begins.

## Diagnostics Options

`DiagnosticsOptions.EnableDiagnostics` enables collection of diagnostic
records. `DiagnosticsOptions.IncludeRawHtml` controls raw HTML capture, and
`DiagnosticsOptions.MaxRawHtmlLength` caps captured payload length.

Raw HTML is omitted by default. The same opt-in also allows renderer image
diagnostics to include capped raw image source context. Without the opt-in,
image diagnostics use bounded display source values only.

## Internal Mapping

`Html2x` maps public options into stage-owned settings:

- `StyleBuildSettings`
- `LayoutBuildSettings`
- `LayoutGeometryRequest`
- `PaginationOptions`
- internal `PdfRenderSettings`

Internal stages consume those settings instead of public option objects.
