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

The PDF renderer consumes render model fragments and page models as read-only facts. `HtmlLayout.Pages` is exposed as a read-only list at this seam. It must not reach back into DOM, style tree, box tree objects, fragment tree building, layout implementation packages, or font source adapters.

The renderer consumes internal renderer-owned `PdfRenderSettings` and does not
reference public converter options. It references `Html2x.Text` only for
internal typeface loading seams and must not perform font source resolution.

`PdfRenderer` is an internal implementation behind `HtmlConverter`.
Dependency-injected renderer constructors that accept filesystem or typeface
factory adapters are internal adapters and must not become public API.

If renderer code needs a value that fragments do not carry, that value belongs
in the layout or fragment contract.

## Render Flow

```text
HtmlLayout
  -> page iteration
  -> page size and Skia page creation validation
  -> fragment dispatch
  -> paint command resolution
  -> Skia PDF canvas drawing
  -> PDF bytes
```

The renderer fails if a page has a non-finite or non-positive size, or if
SkiaSharp cannot begin a PDF page. It must not silently skip a requested page.

## Paint Ordering

Paint command resolution emits page background first, then fragment commands in
stable traversal order and sorts by `ZOrder` followed by command index.

Standard block fragments emit background, border, then child content in
fragment order. Image fragments emit image content and then image border when a
border is present. Rule fragments emit a rule command. Table fragments use a
specialized order: table, row, and cell backgrounds first, then table, row, and
cell borders, then cell content.

Unsupported fragment or paint command runtime types fail explicitly instead of
being silently skipped. Layout diagnostics snapshots use the bounded
`unsupported` fragment kind for unknown fragment subclasses.

## Fonts And Images

Text runs must carry `ResolvedFont` facts before rendering. The renderer loads
the referenced typefaces through renderer-local font cache behavior and does
not resolve fonts through `IFontSource`.

When rendering through `HtmlConverter`, images are loaded from the
conversion-scoped resource store that also supplied layout metadata. Renderer
tests can exercise the fallback `Html2x.Resources` path with renderer-owned
resource settings. The renderer consumes image source, status, rectangle,
padding, and border facts from render model fragments. It does not rederive
layout geometry.

## Diagnostics

The renderer emits render lifecycle records and renderer-owned diagnostics
through `IDiagnosticsSink`. Render summary fields include page count and PDF
byte size after `PdfRender` succeeds.
