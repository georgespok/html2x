# Html2x

Html2x is a modern, cross-platform .NET 8 library for converting static HTML and CSS into PDF. The architecture separates parsing, style computation, layout, fragments, pagination, diagnostics, and rendering so each stage can be tested and extended independently.

## Goals

- Convert business-report HTML and CSS into deterministic PDF output.
- Keep the implementation pure .NET, using AngleSharp for HTML/CSS parsing and SkiaSharp for PDF rendering.
- Preserve clear module boundaries between public API, layout, diagnostics, and rendering.
- Make unsupported input observable through diagnostics instead of silent behavior drift.

## Repository Layout

```text
src/
  Html2x/                     Public composition facade
  Html2x.Abstractions/        Shared contracts, options, diagnostics, measurements
  Html2x.Diagnostics/         Diagnostics JSON serialization
  Html2x.LayoutEngine/        HTML/CSS to box, fragment, and page layout
  Html2x.Renderers.Pdf/       Fragment to PDF rendering
  Tests/
    Html2x.LayoutEngine.Test/
    Html2x.Renderers.Pdf.Test/
    Html2x.Test/
    Html2x.TestConsole/
    Html2x.TestConsole.Test/
docs/                         Developer documentation
build/                        Local generated artifacts
```

## Build And Test

Run commands from the repository root.

```powershell
dotnet restore src/Html2x.sln
dotnet build src/Html2x.sln -c Release
dotnet test src/Html2x.sln -c Release
```

Manual PDF smoke test:

```powershell
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- src/Tests/Html2x.TestConsole/html/example.html build/example.pdf
```

## Minimal Usage

```csharp
using Html2x;
using Html2x.Abstractions.Options;

var converter = new HtmlConverter();

var result = await converter.ToPdfAsync(
    "<h1>Invoice</h1><p>Total: $42.00</p>",
    new HtmlConverterOptions
    {
        Pdf =
        {
            FontPath = @"C:\Projects\html2x\src\Tests\Html2x.TestConsole\fonts"
        }
    });

await File.WriteAllBytesAsync("invoice.pdf", result.PdfBytes);
```

`PdfOptions.FontPath` must point to a font file or directory before layout begins.

## Documentation

Start with the [developer documentation index](docs/README.md). It links the architecture, internals, development, extension, and reference docs.

Key entry points:

- [Getting Started](docs/getting-started.md)
- [Architecture Overview](docs/architecture/overview.md)
- [Processing Pipeline](docs/architecture/pipeline.md)
- [Coding Standards](docs/development/coding-standards.md)
- [Testing](docs/development/testing.md)
- [Supported HTML And CSS](docs/reference/supported-html-css.md)
- [Public API](docs/reference/public-api.md)

## Scope

Supported scope includes static HTML/CSS, block and inline flow, basic tables, lists, images, pagination, borders, backgrounds, colors, fonts, diagnostics, and PDF rendering.

Out of scope includes JavaScript execution, dynamic DOM mutation, browser-compatible layout fidelity, full CSS grid/flex support, accessibility tagging, PDF forms, and browser engine embedding.

## License

MIT License
