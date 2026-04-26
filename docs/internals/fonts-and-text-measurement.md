# Fonts And Text Measurement

Font handling is shared across layout measurement and PDF rendering so text width decisions stay consistent.

## Converter-Owned Font Source

`HtmlConverter` requires `PdfOptions.FontPath`. The path may point to a font file or a directory. The converter creates a `FontPathSource` and shares it with:

- `SkiaTextMeasurer` during layout.
- Fragment text runs produced by layout.
- `PdfRenderer` and `SkiaFontCache` during rendering.

This keeps font selection consistent across measurement and drawing.

## Direct Renderer Usage

Renderer-local fallback is allowed only for direct renderer usage where no converter-owned `IFontSource` exists. Converter flows should use the shared font source.

## Diagnostics

Font resolution diagnostics should include:

- Owner.
- Consumer.
- Requested family, weight, and style when available.
- Configured font path.
- Resolved source.
- Outcome.

## Failure Modes

- Missing `PdfOptions.FontPath` fails before layout begins.
- Invalid font paths fail before layout begins.
- Platform font differences can cause layout drift. Use repository test fonts for deterministic test and console scenarios.
