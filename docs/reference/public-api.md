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
        BaseDirectory = htmlDirectory,
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

## Result

`Html2PdfResult` contains:

- `PdfBytes`: rendered PDF bytes.
- `DiagnosticsReport`: optional diagnostics report when enabled.
