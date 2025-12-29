# Phase 0 Research: Font Accurate Text Measurement

## Decision 1: Measurer placement

- Decision: Implement the concrete measurer in the composition layer and inject it into layout via abstractions.
- Rationale: Keeps Html2x.LayoutEngine isolated and ensures the measurer can be replaced without touching layout.
- Alternatives considered: Put the measurer inside the renderer or layout engine (rejected due to dependency direction and testability).

## Decision 2: Diagnostic payloads

- Decision: Emit diagnostics for font resolution failures, invalid font files, and measurement inputs used for layout decisions.
- Rationale: Supports auditability and quick triage without inspecting rendered PDFs.
- Alternatives considered: Minimal or no diagnostics (rejected because it hides layout failures).

## Decision 3: Wrapping behavior

- Decision: Space-first wrapping with character-level fallback when a token cannot fit an empty line.
- Rationale: Matches expected text layout behavior and keeps long words from breaking layout.
- Alternatives considered: Always character-level wrapping (rejected due to poor readability), hyphenation (deferred).

## Baseline: Current heuristic measurements

- FontMetricsProvider in `src/Html2x.LayoutEngine/FontMetricsProvider.cs` uses a fixed 80/20 split for ascent and descent: ascent = sizePt * 0.8, descent = sizePt * 0.2.
- FontMetricsProvider.MeasureTextWidth uses a rough estimate: width = text.Length * sizePt * 0.5.
- DefaultTextWidthEstimator in `src/Html2x.LayoutEngine/DefaultTextWidthEstimator.cs` delegates width estimation to IFontMetricsProvider.MeasureTextWidth.

## Baseline: Inline measurement data flow

- InlineFragmentStage in `src/Html2x.LayoutEngine/Fragment/Stages/InlineFragmentStage.cs` builds LineBoxFragment items and sets BaselineY and LineHeight using TextRun metrics.
- TextRunFactory in `src/Html2x.LayoutEngine/Fragment/TextRunFactory.cs` supplies TextRun.AdvanceWidth, Ascent, and Descent based on FontMetricsProvider and DefaultTextWidthEstimator.
- Inline fragments currently never wrap; each LineBoxFragment is extended by appending runs without width checks.
