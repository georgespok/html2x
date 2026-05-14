# Public API

This page describes the supported public API surface for converting HTML to PDF
and consuming diagnostics.

## Convert HTML To PDF

The main public entry point is `HtmlConverter`.

```csharp
using Html2x;
using Html2x.Options;

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

With the default converter dependencies, `HtmlConverterOptions.Fonts.FontPath` is
required. It must point to an existing font file or directory before layout
begins.

Missing or invalid font paths throw `InvalidOperationException`. When
diagnostics are enabled, the exception carries the diagnostics report in
`Exception.Data["DiagnosticsReport"]`.

## Advanced Converter Dependencies

Advanced in-process callers can provide narrow adapter dependencies through
`HtmlConverterDependencies`.

```csharp
using Html2x;

var converter = new HtmlConverter(new HtmlConverterDependencies
{
    FontSourceFactory = () => CreateCustomFontSource()
});
```

Supported dependency factories are:

- `FontSourceFactory`: creates a conversion-scoped `IFontSource` used by the
  built-in Skia text measurer. When supplied,
  `HtmlConverterOptions.Fonts.FontPath` is not required.
- `TextMeasurerFactory`: creates a conversion-scoped `ITextMeasurer`. The
  measurer must implement `Measure(FontKey font, float sizePt, string text)`.
  Each call must return complete `TextMeasurement` facts: finite non-negative
  width, ascent, and descent values plus a resolved font with a non-empty source
  id.

If both factories are supplied, `TextMeasurerFactory` is used and
`FontSourceFactory` is ignored. The converter owns the returned adapter for the
single conversion and disposes it when it implements `IDisposable`.
Factories must return non-null adapters. After option validation succeeds, if a
factory returns null or throws before layout begins, diagnostics-enabled
conversions attach a diagnostics report to the original exception and record the
failure as a configuration failure.
`HtmlConverterDependencies` is not a service container. Layout algorithms, mutable
boxes, style trees, image byte loading, published layout facts, and renderer
internals are not public extension points.

Html2x validates the structural shape of custom text measurement results near
the layout boundary. It does not validate renderer font file loadability there;
the PDF renderer validates that `ResolvedFont.FilePath` can be loaded when it
renders text.

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
payload is capped by `DiagnosticsOptions.MaxRawHtmlLength`. The same opt-in
allows renderer image diagnostics to include capped raw image source context;
otherwise image diagnostics expose only a bounded display source.

## Result

`HtmlToPdfResult` contains:

- `PdfBytes`: rendered PDF bytes. Each read returns a defensive copy so caller
  mutation cannot change the stored result.
- `DiagnosticsReport`: optional diagnostics report when enabled.

## Public Surface

The supported consumer facade is `HtmlConverter`, `HtmlConverterDependencies`,
`HtmlToPdfResult`, and option types under `Html2x.Options`.

Public contract classification:

- Consumer facade: `HtmlConverter`, `HtmlConverterDependencies`,
  `HtmlToPdfResult`, and option types under `Html2x.Options`.
- Public value facts needed by options and runtime seams, including page size
  values and text measurement/font facts.
- Diagnostics surface: diagnostics contracts plus `DiagnosticsCollector`,
  `DiagnosticsReport`, and `DiagnosticsReportSerializer`.
- Advanced dependency seams: `IFontSource`, `FontPathSource`,
  `ITextMeasurer`, `SkiaTextMeasurer`, `TextMeasurement`, and
  `FontResolutionException`. Custom adapters are created through
  converter-scoped dependency factories.

`Html2x.LayoutEngine.Contracts` is an internal pipeline handoff assembly. Style
trees, geometry requests, image metadata resolver contracts, published layout
facts, and diagnostic snapshot mappers are not consumer extension points.

The PDF renderer, render model documents, page models, and fragment types are
internal implementation surface. They are not supported consumer extension
points.
