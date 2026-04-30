# Public API

The main public entry point is `HtmlConverter`.

## Convert HTML To PDF

```csharp
using Html2x;
using Html2x.Abstractions.Options;

var converter = new HtmlConverter();

var result = await converter.ToPdfAsync(
    "<p>Hello</p>",
    new HtmlConverterOptions
    {
        Pdf =
        {
            FontPath = @"C:\Projects\html2x\src\Tests\Html2x.TestConsole\fonts"
        }
    });

await File.WriteAllBytesAsync("output.pdf", result.PdfBytes);
```

## `HtmlConverterOptions`

`HtmlConverterOptions` groups:

- `Layout`: layout options such as page size, HTML directory, image size limit, and user agent stylesheet behavior.
- `Pdf`: PDF options such as font path, page size, HTML directory, image size limit, and debug flag.
- `Diagnostics`: diagnostics enablement.

## Required Font Path

`PdfOptions.FontPath` is required. It must point to an existing font file or directory before layout begins.

Missing or invalid font paths throw `InvalidOperationException`. When
diagnostics are enabled, the exception carries the diagnostics report in
`Exception.Data["DiagnosticsReport"]`.

## Diagnostics

```csharp
var result = await converter.ToPdfAsync(
    html,
    new HtmlConverterOptions
    {
        Pdf = { FontPath = fontPath },
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
