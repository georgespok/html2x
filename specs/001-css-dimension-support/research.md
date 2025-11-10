# Research: CSS Height and Width Support

## Decision 1: Supported Units = px, pt, percent
- **Decision**: Accept only px, pt, and percentage values for `width`/`height` in this phase; all other units fall back to auto with warnings.
- **Rationale**: Html2x already uses device-independent units (points) internally, so px/pt mapping plus percentage resolution is low risk and testable; broader CSS units (cm, mm, em, viewport) require additional font metrics or page context not yet modeled.
- **Alternatives considered**: *(a)* Full CSS absolute unit support (cm/in/mm) — rejected because QuestPDF mapping would need physical DPI guarantees we cannot test today. *(b)* Relative units (em/rem) — rejected because inline font-size inheritance would expand scope into typography rework.

## Decision 2: Percentage Resolution Requires Single Reflow
- **Decision**: Resolve percentage widths/heights against the containing block; if the container lacks explicit dimensions, perform one additional measurement pass to converge or log a warning if still undefined.
- **Rationale**: Keeps deterministic outcomes while avoiding infinite loops; a single extra measurement matches current layout performance constraints and mirrors browser behavior for simple block flows.
- **Alternatives considered**: *(a)* Recursive reflow until convergence — rejected for performance unpredictability. *(b)* Immediate fallback to auto — rejected because it would surprise designers relying on percentage layouts.

## Decision 3: Structured Diagnostics Capture Dimension Lineage
- **Decision**: Extend the diagnostics payload with `elementId`, `requestedWidth`, `requestedHeight`, `resolvedWidth`, `resolvedHeight`, `unit`, and `fallbackReason` fields emitted at the fragment stage.
- **Rationale**: Aligns with Constitution Principle IV by giving support engineers enough metadata to spot sizing regressions without re-rendering PDFs.
- **Alternatives considered**: *(a)* Logging only warnings — rejected because happy-path dimension data is necessary for triage. *(b)* Deferring to renderer logging — rejected since renderer must remain a consumer, not a decision-maker.

## Decision 4: Test Strategy = Layout + Renderer + Harness
- **Decision**: Introduce parameterized unit tests in `Html2x.Layout.Test` for style resolution, snapshot fragment comparisons in `Html2x.Pdf.Test`, and a Pdf.TestConsole script that renders the bordered-block grid fixture to visually confirm deterministic output.
- **Rationale**: Satisfies Principle III (behavior-focused tests) and Principle II (deterministic cross-platform guarantees) while keeping coverage close to affected modules.
- **Alternatives considered**: *(a)* Rely solely on integration tests — rejected because failures would be harder to localize. *(b)* Rely solely on unit tests — rejected because renderer interaction and pagination must be validated end-to-end.
