# Quickstart: SkiaSharp Renderer

## Build
1) `dotnet restore Html2x.sln`
2) `dotnet build Html2x.sln -c Release`

## Render via library
```csharp
using Html2x.Abstractions;
using Html2x.Renderers.Pdf.Skia; // new renderer namespace

var fragments = GetFragmentsFromLayout(); // absolute x/y/width/height
var page = new PageSize(width: 595, height: 842, dpi: 72);
var renderer = new SkiaPdfRenderer();

await using var output = File.Create("build/sample.pdf");
renderer.Render(new RenderRequest(page, fragments, options: new RenderOptions
{
    EmbedFonts = true,
    ColorSpace = ColorSpace.Srgb
}), output);
```

## Render via console harness
1) Prepare an HTML sample (e.g., `src/Tests/Html2x.TestConsole/html/skia-sample.html`).
2) `dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- --input src/Tests/Html2x.TestConsole/html/skia-sample.html --output build/skia-sample.pdf`

## Diagnostics check
- Inspect geometry logs emitted by LayoutEngine; renderer should not change coordinates.
- Renderer logs missing/oversize images and font fallbacks from layout diagnostics; failures include fragment id, page, command.

## Test posture
- Run `dotnet test Html2x.sln -c Release`.
- Renderer tests marked for migration may be temporarily skipped; track skips with reasons until Skia path is green.
