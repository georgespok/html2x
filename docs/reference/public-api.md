# Public API

This page describes the supported public API surface for converting HTML to PDF
and consuming diagnostics.

## Convert HTML To PDF

The main public entry point is `HtmlConverter`.

```csharp
using Html2x;

var converter = new HtmlConverter();

var result = await converter.ToPdfAsync(
    "<p>Hello</p>",
    new HtmlConverterOptions
    {
        Fonts = new FontOptions
        {
            FontPath = @"C:\Projects\html2x\src\Tests\Html2x.TestConsole\fonts"
        }
    });

await File.WriteAllBytesAsync("output.pdf", result.PdfBytes);
```

`ToPdfAsync` also accepts a `CancellationToken`.

## Default Font Path Requirement

With the default converter runtime, `HtmlConverterOptions.Fonts.FontPath` is
required. It must point to an existing font file or directory before layout
begins.

Missing or invalid font paths throw `InvalidOperationException`. When
diagnostics are enabled, the exception carries the diagnostics report in
`Exception.Data["DiagnosticsReport"]`.

## Advanced Runtime Construction

Advanced in-process callers can provide a narrow runtime adapter set through
`HtmlConverterRuntime`.

```csharp
using Html2x;

var converter = new HtmlConverter(new HtmlConverterRuntime
{
    FontSource = customFontSource
});
```

Supported runtime adapters are:

- `FontSource`: custom `IFontSource` used by the built-in Skia text measurer.
  When supplied, `HtmlConverterOptions.Fonts.FontPath` is not required. The
  caller owns the font source lifetime.
- `TextMeasurer`: custom `ITextMeasurer`. The caller owns its lifetime and must
  return complete `TextMeasurement` facts, including resolved fonts suitable for
  PDF rendering.

If both are supplied, `TextMeasurer` is used and `FontSource` is ignored.
`HtmlConverterRuntime` is not a service container. Layout algorithms, mutable
boxes, style trees, image byte loading, published layout facts, and renderer
internals are not public extension points.

## Diagnostics

```csharp
var result = await converter.ToPdfAsync(
    html,
    new HtmlConverterOptions
    {
        Fonts = new FontOptions { FontPath = fontPath },
        Diagnostics = new DiagnosticsOptions { EnableDiagnostics = true }
    });

var report = result.DiagnosticsReport;
```

Use `Html2x.Diagnostics.DiagnosticsReportSerializer.ToJson(report)` to export
diagnostics JSON.

Raw HTML is omitted from diagnostics by default. To include it for local
troubleshooting, set `DiagnosticsOptions.IncludeRawHtml = true`. The captured
payload is capped by `DiagnosticsOptions.MaxRawHtmlLength`.

## Result

`Html2PdfResult` contains:

- `PdfBytes`: rendered PDF bytes. Each read returns a defensive copy so caller
  mutation cannot change the stored result.
- `DiagnosticsReport`: optional diagnostics report when enabled.

## Public Surface

The supported consumer facade is `HtmlConverter`, `HtmlConverterRuntime`,
`HtmlConverterOptions`, and `Html2PdfResult`.

Public contract classification:

- Consumer facade: `HtmlConverter`, `HtmlConverterRuntime`,
  `Html2PdfResult`, and option types under `Html2x.Options`.
- Renderer-author facts: `HtmlLayout`, `LayoutPage`, fragment types, render
  geometry value types, render style value types, and text facts under
  `Html2x.RenderModel`.
- Renderer entry point: `PdfRenderer` and `PdfRenderSettings`.
- Diagnostics surface: diagnostics contracts plus `DiagnosticsCollector`,
  `DiagnosticsReport`, and `DiagnosticsReportSerializer`.
- Advanced runtime seams: `IFontSource`, `FontPathSource`,
  `DiagnosticsFontSource`, `ITextMeasurer`, `SkiaTextMeasurer`,
  `TextMeasurement`, and `FontResolutionException`.

`Html2x.LayoutEngine.Contracts` is an internal pipeline handoff assembly. Style
trees, geometry requests, image metadata resolver contracts, published layout
facts, and diagnostic snapshot mappers are not consumer extension points.

## Direct Renderer Usage

`HtmlLayout.Pages` is read-only for consumers and renderers. Code that manually
builds an `HtmlLayout` for advanced renderer usage should add pages through
`HtmlLayout.AddPage` or the `HtmlLayout(IEnumerable<LayoutPage>)` constructor.

Direct `PdfRenderer.RenderAsync` usage validates `PdfRenderSettings`.
`PdfRenderSettings.MaxImageSizeBytes` must be greater than zero and invalid
values throw before rendering begins.

Text runs passed directly to the renderer must include `ResolvedFont`.
