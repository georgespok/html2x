# Html2x

Html2x is a modern, cross-platform .NET 8 library for converting static HTML and CSS into PDF. The architecture separates parsing, style computation, layout, fragments, pagination, diagnostics, and rendering so each stage can be tested and extended independently.

## Goals

- Convert business-report HTML and CSS into deterministic PDF output.
- Keep the implementation pure .NET, using AngleSharp for HTML/CSS parsing and SkiaSharp for PDF rendering.
- Preserve clear module boundaries between public API, layout, diagnostics, and rendering.
- Make unsupported input observable through diagnostics instead of silent behavior drift.

## Repository Guidance

The current project map, module responsibilities, and build commands live in
[developer documentation](docs/README.md). Agent workflow rules live in
[AGENTS.md](AGENTS.md).

Primary source documents:

- [Architecture Overview](docs/architecture/overview.md): project map, module
  ownership, and primary data flow.
- [Getting Started](docs/getting-started.md): restore, build, and test
  commands.
- [Testing](docs/development/testing.md): test project ownership, practices, and
  focused commands.
- [Coding Standards](docs/development/coding-standards.md): implementation
  conventions, naming, layering, diagnostics, and review checks.
- [Agent Guidance](AGENTS.md): feature records, validation, commits, PRs,
  local artifacts, and shell guidance.

For public API, supported HTML/CSS, internals, and extension docs, start with
the [developer documentation index](docs/README.md).

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

`HtmlConverterOptions.Fonts.FontPath` must point to a font file or directory before layout begins.

## Scope

Supported scope includes static HTML/CSS, block and inline flow, basic tables, lists, images, pagination, borders, backgrounds, colors, fonts, diagnostics, and PDF rendering.

Out of scope includes JavaScript execution, dynamic DOM mutation, browser-compatible layout fidelity, full CSS grid/flex support, accessibility tagging, PDF forms, and browser engine embedding.

## License

MIT License
