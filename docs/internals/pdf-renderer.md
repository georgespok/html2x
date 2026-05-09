# PDF Renderer

This page explains how `Html2x.Renderers.Pdf` renders
`Html2x.RenderModel.HtmlLayout` pages to PDF bytes using SkiaSharp.

## Responsibilities

- Create the Skia PDF document.
- Convert page fragments into paint operations.
- Draw backgrounds, borders, text, images, rules, tables, rows, and cells.
- Load typefaces from `TextRun.ResolvedFont` facts carried by renderer input.
- Emit renderer diagnostics and render summaries.

## Boundary

The PDF renderer consumes render model fragments and page models as read-only facts. `HtmlLayout.Pages` is exposed as a read-only list at this seam. It must not reach back into DOM, style tree, box tree objects, fragment projection, layout implementation packages, or font source adapters.

The renderer consumes renderer-owned `PdfRenderSettings` and does not reference
public converter options. It references `Html2x.Text` only for internal
typeface loading seams and must not perform font source resolution.

`PdfRenderer()` is the public construction path. Dependency-injected renderer
constructors that accept filesystem or typeface factory adapters are internal
adapters and must not become public API.

If renderer code needs a value that fragments do not carry, that value belongs
in the layout or fragment contract.

## Render Flow

```text
HtmlLayout
  -> page iteration
  -> fragment dispatch
  -> paint command resolution
  -> Skia PDF canvas drawing
  -> PDF bytes
```

## Paint Ordering

Current rendering preserves established visual order:

1. Page background.
2. Block backgrounds.
3. Borders.
4. Images and image borders.
5. Rules.
6. Text.
7. Table backgrounds and borders in table, row, cell, then content order.

`ZOrder` can be carried as metadata, but stacking behavior must be explicit in
the render model contract.

## Fonts And Images

Text runs must carry `ResolvedFont` facts before rendering. The renderer loads
the referenced typefaces through renderer-local font cache behavior and does
not resolve fonts through `IFontSource`.

When rendering through `HtmlConverter`, images are loaded from the
conversion-scoped resource store that also supplied layout metadata. Direct
`PdfRenderer` usage falls back to `Html2x.Resources` with renderer-owned
resource settings. The renderer consumes image source, status, rectangle,
padding, and border facts from render model fragments. It does not rederive
layout geometry.

## Diagnostics

The renderer emits render lifecycle records and renderer-owned diagnostics
through `IDiagnosticsSink`. Render summary fields include page count and PDF
byte size after `PdfRender` succeeds.
