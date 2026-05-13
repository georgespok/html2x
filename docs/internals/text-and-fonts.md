# Text And Fonts

This page explains font resolution, text measurement, resolved font facts, and
renderer font requirements.

## Ownership

`Html2x.Text` owns text measurement contracts and font resolution contracts.
Pure font facts such as `FontKey`, `FontWeight`, `FontStyle`, and
`ResolvedFont` live in `Html2x.RenderModel`.

## Converter-Owned Font Source

With the default converter dependencies, `HtmlConverter` requires
`HtmlConverterOptions.Fonts.FontPath`. The path may point to a font file or a
directory. The converter creates a `FontPathSource` and passes it to:

- An internal diagnostics font source wrapper when diagnostics are enabled.
- `SkiaTextMeasurer` during layout.

Geometry publishes the resulting `ResolvedFont` facts on normal pipeline text
runs. Fragment tree building copies those facts, and PDF rendering loads typefaces
from them without resolving fonts again.

## Font And Text Dependency Factories

`HtmlConverterDependencies` is the supported advanced in-process construction path.
It may supply:

- `FontSourceFactory`, which creates a conversion-scoped `IFontSource` used by
  the built-in `SkiaTextMeasurer`.
- `TextMeasurerFactory`, which creates a conversion-scoped `ITextMeasurer` used
  directly for layout measurement.

When a dependency font source factory is supplied, `FontOptions.FontPath` is not
required. Dependency adapters are converter-scoped: the converter calls the
chosen factory once per conversion and disposes the returned adapter when it
implements `IDisposable`. A dependency text measurer must return complete
`TextMeasurement` values from one `Measure` call. Width, ascent, descent, and
`ResolvedFont` must be available together. Html2x validates the structural
measurement facts before layout consumes them: measured values must be finite
and non-negative, the resolved font must be present, and the resolved font
source id must not be empty. The dependency surface does not expose layout
boxes, style trees, image byte loading, or renderer state.
Factory creation is configuration work. A dependency factory that returns null
or throws before layout begins fails the conversion before layout starts, and
diagnostics-enabled conversions attach the diagnostics report to the original
exception.

Resolved font file loadability remains renderer-owned. A custom text measurer
can pass layout validation with a structurally valid `ResolvedFont`, but PDF
rendering still requires a loadable `ResolvedFont.FilePath`.

## Renderer Font Loading

Renderer inputs must provide `TextRun.ResolvedFont` on every text run.
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
  the default dependencies have no custom font source or text measurer factory.
- Invalid font paths fail before layout begins.
- Invalid custom text measurement values fail before layout consumes the
  measurement.
- Font path and renderer font failures use `FontResolutionException` with
  typed request, resolved font, configured path, resolved path, and text facts.
- Missing `TextRun.ResolvedFont` fails during PDF rendering with a renderer
  input error.
- Platform font differences can cause layout drift. Use a stable bundled or
  absolute font set when deterministic output matters.
