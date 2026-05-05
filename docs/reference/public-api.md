# Public API

The main public entry point is `HtmlConverter`.

## Convert HTML To PDF

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

## `HtmlConverterOptions`

`HtmlConverterOptions` groups:

- `Page`: page-level conversion options such as page size.
- `Resources`: resource loading options such as base directory and image size limit.
- `Css`: CSS processing options such as user agent stylesheet behavior.
- `Fonts`: font resolution options.
- `Diagnostics`: diagnostics enablement.

## Required Font Path

`HtmlConverterOptions.Fonts.FontPath` is required. It must point to an existing font file or directory before layout begins.

Missing or invalid font paths throw `InvalidOperationException`. When
diagnostics are enabled, the exception carries the diagnostics report in
`Exception.Data["DiagnosticsReport"]`.

## Shared Conversion Facts

Set shared facts once on the public conversion request. `HtmlConverter` maps
those values into stage-owned layout, style, and PDF render settings.

```csharp
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
    }
};
```

If `Resources.BaseDirectory` is not set, the converter uses
`AppContext.BaseDirectory`. Set it explicitly when HTML references relative
image paths.

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

`HtmlLayout.Pages` is read-only for consumers and renderers. Code that manually
builds an `HtmlLayout` for advanced renderer usage should add pages through
`HtmlLayout.AddPage` or the `HtmlLayout(IEnumerable<LayoutPage>)` constructor.
`HtmlLayout` is sealed. Page mutation remains limited to explicit construction
methods because renderer authors may manually assemble layouts.

## Public Surface

The supported consumer facade is `HtmlConverter`, `HtmlConverterOptions`, and
`Html2PdfResult`. `Html2x.RenderModel` remains public for direct renderer input
and custom renderer authors.

Public contract classification:

- Consumer facade: `HtmlConverter`, `Html2PdfResult`, and option types under
  `Html2x.Options`.
- Renderer-author facts: `HtmlLayout`, `LayoutPage`, fragment types, render
  geometry value types, render style value types, and
  text facts under `Html2x.RenderModel`.
- Renderer entry point: `PdfRenderer` and `PdfRenderSettings`.
- Diagnostics surface: diagnostics contracts plus `DiagnosticsCollector`,
  `DiagnosticsReport`, and `DiagnosticsReportSerializer`.
- Advanced runtime seams: `IFontSource`, `FontPathSource`,
  `DiagnosticsFontSource`, `ITextMeasurer`, `SkiaTextMeasurer`,
  `TextMeasurement`, and `FontResolutionException`.

`Html2x.LayoutEngine.Contracts` is an internal pipeline handoff assembly. Style
trees, geometry requests, image metadata resolver contracts, published layout
facts, and diagnostic snapshot mappers are not consumer extension points.

Text runtime seams in `Html2x.Text` are intentionally public for advanced
manual render model construction and tests: `IFontSource`, `FontPathSource`,
`DiagnosticsFontSource`, `ITextMeasurer`, `SkiaTextMeasurer`,
`TextMeasurement`, and `FontResolutionException`. Public constructors keep
filesystem and Skia factory details internal.

## Compatibility Cleanup Stance

`HtmlConverter`, `Html2PdfResult`, `HtmlLayout`, and `PdfRenderer` are sealed
because subclassing them is not a supported extension model. This is an
intentional public source compatibility break for accidental inheritance.

`DiagnosticFields` and `DiagnosticObject` expose dictionary-like reads through
indexers, `ContainsKey`, keys, values, count, and enumeration. They no longer
implement `IReadOnlyDictionary` because the public `TryGetValue(..., out ...)`
shape conflicted with the no by-reference production signature rule.

The fragment model is a closed built-in set for the Html2x pipeline. `Fragment`
and `BlockFragment` remain inheritable for source compatibility with existing
manual renderer fixtures, but custom subclasses are unsupported by pagination
and PDF rendering. A future major cleanup may close these types after replacing
that compatibility shape with an explicit construction API.

`HtmlLayout` no longer exposes document metadata because the built-in PDF
renderer did not consume those values. `VisualStyle` exposes only renderer facts
that are currently consumed or intentionally carried through the render model.
`ColorRgba` is a pure renderer-facing value and no longer exposes `FromCss`;
CSS color parsing is owned by the style stage before render facts are produced.

`ImageLoadStatus` is the canonical image outcome used by resource loading,
layout metadata, image fragments, and PDF diagnostics. `ImageFragment.IsMissing`
and `ImageFragment.IsOversize` are derived from `ImageFragment.Status`; callers
constructing image fragments manually should set `Status` instead of trying to
maintain separate status flags.

Direct `PdfRenderer.RenderAsync` usage validates `PdfRenderSettings`.
`PdfRenderSettings.MaxImageSizeBytes` must be greater than zero and invalid
values throw before rendering begins.
