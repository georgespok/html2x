# Html2x

Html2x is a modern, cross-platform .NET 8 library for converting static HTML and CSS into PDF. The architecture separates parsing, style computation, layout, fragments, pagination, diagnostics, and rendering so each stage can be tested and extended independently.

## Goals

- Convert business-report HTML and CSS into deterministic PDF output.
- Keep the implementation pure .NET, using AngleSharp for HTML/CSS parsing and SkiaSharp for PDF rendering.
- Preserve clear module boundaries between public API, layout, diagnostics, and rendering.
- Make unsupported input observable through diagnostics instead of silent behavior drift.

## Documentation

The durable project map, module responsibilities, public API, supported scope,
runtime behavior, and developer commands live in
[project documentation](docs/README.md). Local agent workflow rules live in
[AGENTS.md](AGENTS.md).

Primary source documents:

- [Getting Started](docs/getting-started.md): first-run path for a clean
  checkout.
- [Architecture Overview](docs/architecture/overview.md): project map and
  primary data flow.
- [Processing Pipeline](docs/architecture/pipeline.md): HTML/CSS input through
  PDF bytes.
- [Public API](docs/reference/public-api.md): converter usage, options,
  diagnostics, and failure behavior.
- [Supported HTML And CSS](docs/reference/supported-html-css.md): current
  behavior and explicit limitations.
- [Diagnostics Events](docs/reference/diagnostics-events.md): emitted
  diagnostic events and payload shapes.
- [Agent Guidance](AGENTS.md): local workflow routing for contributors and
  agents.

For concepts, architecture, internals, extension paths, public API, supported
HTML/CSS, diagnostics, and troubleshooting, start with the
[project documentation index](docs/README.md).

## Minimal Usage

```csharp
using Html2x;

var converter = new HtmlConverter();

var result = await converter.ToPdfAsync(
    "<h1>Invoice</h1><p>Total: $42.00</p>",
    new HtmlConverterOptions
    {
        Fonts = new FontOptions
        {
            FontPath = @"C:\Projects\html2x\src\Tests\Html2x.TestConsole\fonts"
        }
    });

await File.WriteAllBytesAsync("invoice.pdf", result.PdfBytes);
```

With the default converter dependencies, `HtmlConverterOptions.Fonts.FontPath`
must point to a font file or directory before layout begins. Advanced
in-process callers may provide approved font or text adapter factories through
`HtmlConverterDependencies`.

## Scope

Supported scope includes static HTML/CSS, block and inline flow, basic tables, lists, images, pagination, borders, backgrounds, colors, fonts, diagnostics, and PDF rendering.

Out of scope includes JavaScript execution, dynamic DOM mutation, browser-compatible layout fidelity, full CSS grid/flex support, accessibility tagging, PDF forms, and browser engine embedding.

## License

MIT License
