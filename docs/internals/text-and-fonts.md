# Text And Fonts

This page explains font resolution, text measurement, resolved font facts, and
renderer font requirements.

## Ownership

`Html2x.Text` owns text measurement contracts and font resolution contracts.
Pure font facts such as `FontKey`, `FontWeight`, `FontStyle`, and
`ResolvedFont` live in `Html2x.RenderModel`.

## Converter-Owned Font Source

With the default runtime, `HtmlConverter` requires
`HtmlConverterOptions.Fonts.FontPath`. The path may point to a font file or a
directory. The converter creates a `FontPathSource` and passes it to:

- `DiagnosticsFontSource` when diagnostics are enabled.
- `SkiaTextMeasurer` during layout.

Geometry publishes the resulting `ResolvedFont` facts on normal pipeline text
runs. Fragment projection copies those facts, and PDF rendering loads typefaces
from them without resolving fonts again.

## Runtime Font And Text Adapters

`HtmlConverterRuntime` is the supported advanced in-process construction path.
It may supply:

- `IFontSource`, used by the built-in `SkiaTextMeasurer`.
- `ITextMeasurer`, used directly for layout measurement.

When a runtime font source is supplied, `FontOptions.FontPath` is not required
and the caller owns the font source lifetime. When a runtime text measurer is
supplied, the caller owns the measurer lifetime and must return
`TextMeasurement` values with usable `ResolvedFont` facts. The runtime surface
does not expose layout boxes, style trees, image byte loading, or renderer
state.

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

- Missing `HtmlConverterOptions.Fonts.FontPath` fails before layout begins when
  the default runtime has no custom font source or text measurer.
- Invalid font paths fail before layout begins.
- Font path and renderer font failures use `FontResolutionException` with
  typed request, resolved font, configured path, resolved path, and text facts.
- Missing `TextRun.ResolvedFont` fails during PDF rendering with a renderer
  input error.
- Platform font differences can cause layout drift. Use a stable bundled or
  absolute font set when deterministic output matters.
