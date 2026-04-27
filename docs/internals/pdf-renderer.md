# PDF Renderer

`Html2x.Renderers.Pdf` renders `HtmlLayout` pages to PDF bytes using SkiaSharp.

## Responsibilities

- Create the Skia PDF document.
- Convert page fragments into paint operations.
- Draw backgrounds, borders, text, images, rules, tables, rows, and cells.
- Resolve renderer-local font cache behavior from the converter-provided font source when available.
- Emit renderer diagnostics and render summaries.

## Boundary

The PDF renderer consumes fragments and page models as read-only facts. It must not reach back into DOM, style tree, or box tree objects.

If renderer code needs a value that fragments do not carry, add that value to the layout or fragment contract and update tests across the affected stages.

## Paint Ordering

Current rendering preserves established visual order:

1. Page background.
2. Block backgrounds.
3. Borders.
4. Images and image borders.
5. Rules.
6. Text.
7. Table backgrounds and borders in table, row, cell, then content order.

`ZOrder` can be carried as metadata, but new stacking behavior requires explicit design and tests.

## Testing

Renderer tests should prefer semantic checks:

- PDF is valid.
- Expected page count exists.
- Expected text can be extracted where appropriate.
- Diagnostics report expected unsupported or fallback behavior.

Do not assert binary PDF equality.
