# Fonts And Text Measurement

Font handling behavior is centralized in `Html2x.Text` so layout measurement
and PDF rendering use the same resolved font facts. The pure font facts
themselves live in `Html2x.RenderModel`.

## Converter-Owned Font Source

`HtmlConverter` requires `HtmlConverterOptions.Fonts.FontPath`. The path may point to a font file or a directory. The converter creates a `FontPathSource` and passes it to:

- `DiagnosticsFontSource` when diagnostics are enabled.
- `SkiaTextMeasurer` during layout.

Geometry publishes the resulting `ResolvedFont` facts on normal pipeline text runs. Fragment projection copies those facts, and PDF rendering loads typefaces from them without resolving fonts again. This keeps font selection consistent across measurement and drawing.

The public construction path is intentionally high level:
`FontPathSource(string)` and `SkiaTextMeasurer(IFontSource)`. The filesystem and
Skia typeface factory seams are internal runtime adapters used by text and PDF
tests, not public integration points.

## Direct Renderer Usage

Direct renderer callers must provide `TextRun.ResolvedFont` on every text run.
`SkiaFontCache` loads the referenced font file from those resolved facts. It
does not call `IFontSource` or perform renderer-local fallback resolution.

## Diagnostics

Font resolution diagnostics should include:

- Owner.
- Consumer.
- Requested family, weight, and style when available.
- Configured font path.
- Resolved source.
- Outcome.

## Failure Modes

- Missing `HtmlConverterOptions.Fonts.FontPath` fails before layout begins.
- Invalid font paths fail before layout begins.
- Missing `TextRun.ResolvedFont` fails during PDF rendering with a renderer input error.
- Platform font differences can cause layout drift. Use repository test fonts for deterministic test and console scenarios.
