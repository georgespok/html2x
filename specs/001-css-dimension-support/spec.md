# Feature Specification: CSS Height and Width Support

**Feature Branch**: `001-css-dimension-support`  
**Created**: November 10, 2025  
**Status**: Draft  
**Input**: User description: "implement support for heigh and width css properties"

## User Scenarios & Testing (mandatory)

Constitution alignment: Each story preserves staged layout discipline (Principle I) by capturing width and height inside the style-to-box pipeline, enforces deterministic fragment sizing (Principle II), and specifies harness diagnostics so PDF output variance is immediately observable (Principle IV).

### User Story 1 - Fixed Width Blocks Render Predictably (Priority: P1)

Report designers need block containers (cards, summary rows) to respect CSS `width` and `height` so multi-column layouts match approved mockups.

**Why this priority**: Without predictable sizing the generated PDF breaks tabular balance, making reports unusable for finance approvals.

**Independent Test**: Create a regression fixture in `Html2x.TestConsole` that renders a grid of divs with varied `width`/`height`; capture fragment metrics via layout diagnostics and assert PDF bounding boxes match expected values.

**Constitution alignment**: Style computation produces normalized dimensions before pagination, fragment builder records them, and harness logs include `Box.WidthResolved` and `Box.HeightResolved` for traceability.

**Acceptance Scenarios**:

1. **Given** a block element styled with `width: 320px`, **When** the layout engine computes the fragment tree, **Then** the resulting fragment spans exactly 320px within tolerance on the supported Windows runner.
2. **Given** a block element with `height: 120px` and overflow content, **When** the renderer paginates it, **Then** the box height clips content per overflow rules and emits a diagnostic message identifying truncated nodes.

---

### User Story 2 - Bordered Blocks Show Consistent Footprints (Priority: P2)

Template authors need bordered content blocks (used as placeholders for imagery or KPIs) to honor CSS `width`/`height` so brand frames align across cards and headers even without `<img>` support.

**Why this priority**: Marketing reports rely on fixed visual footprints; inconsistent block sizing forces manual editing and delays publication.

**Independent Test**: Add an integration test where sample HTML renders bordered `<div>` elements with fixed pixel widths and heights, then compare resulting fragment rectangles against golden data captured via the layout verifier.

**Constitution alignment**: Layout stage resolves container sizes before applying borders, renderer consumes only fragment metrics, and structured logging records requested versus resolved dimensions for later audits.

**Acceptance Scenarios**:

1. **Given** a bordered `<div>` without explicit height but with `width: 240px`, **When** the containing block width is computed, **Then** the fragment width locks to 240px before pagination, the border aligns with expected guides, and height remains auto after a single measurement pass.
2. **Given** a bordered `<div>` with `height: 80px` and `width: 200px`, **When** layout resolves the styles, **Then** the engine preserves those dimensions exactly and records the calculated size in the diagnostics stream.

---

### User Story 3 - Invalid Dimension Inputs Fail Gracefully (Priority: P3)

Operators require actionable feedback when authors use unsupported units or conflicting `width`/`height` constraints so they can correct templates without diving into code.

**Why this priority**: Clear feedback reduces support load and prevents silent PDF regressions for downstream compliance teams.

**Independent Test**: Write a negative test that feeds malformed CSS (negative width, unsupported units) and assert the harness surfaces warnings while falling back to auto sizing.

**Constitution alignment**: Style parsing validates inputs without bypassing shared abstractions, deterministic fallbacks keep reproducible output, and structured logs attach template identifiers for observability.

**Acceptance Scenarios**:

1. **Given** a block with `width: -25px`, **When** styles are validated, **Then** the system logs a warning, ignores the invalid value, and defaults to auto width without crashing the layout stage.
2. **Given** a container with both `width` and `max-width` that conflict, **When** layout resolves constraints, **Then** the smaller constraint wins and the decision is recorded in the diagnostics payload returned to the harness.

---

### Edge Cases

- Percentage width requests are unsupported; the parser must warn and fall back to auto sizing without attempting iterative resolution.
- Height resolution stops after the first measurement pass; if a container still depends on unresolved child metrics, log the limitation and keep the element on auto height.
- Height specified on inline elements; treat as no-op but log advisory so designers understand why it was ignored.
- Missing fonts that change text metrics; regression harness compares fragment rectangles rather than glyph counts to keep deterministic assertions valid.
- Cross-culture numeric formats in inline styles; parser normalizes decimal separators before validation and records failures with locale data.

## Requirements (mandatory)

### Functional Requirements

- **FR-001**: The style system must parse `width` and `height` declarations (limited to px and pt units) into normalized layout units before box construction, and emit actionable warnings for any other unit, including percentages, before falling back to auto.
- **FR-002**: The box builder must apply resolved widths and heights to block-level elements (including bordered placeholders) while preserving pipeline ordering; no renderer-specific shortcuts may alter these metrics downstream.
- **FR-003**: Width resolution must honor explicit px/pt declarations and, when the container lacks an explicit dimension, fall back to content-driven auto width while logging the assumption; percentage requests log warnings and default to auto immediately.
- **FR-004**: When only width is provided, the layout engine may derive height from content flow only for simple block containers (no nested dimension dependencies) and must do so within a single measurement pass while keeping variance below 1 pt.
- **FR-005**: Auto height with fixed width may perform exactly one measurement pass; if the pass cannot stabilize within 1 pt or requires re-measurement, the engine logs the limitation, keeps the element on auto height, and marks the run as failed.
- **FR-006**: Validation must handle conflicting constraints (`min/max/explicit`) by applying CSS precedence rules and logging the chosen outcome for audit trails.
- **FR-007**: Structured diagnostics must include element identifier, resolved width, resolved height, unit source, and any fallback decision so QA can assert behavior using existing harness tooling.

### Key Entities (include if feature involves data)

- **Requested Dimension**: Captures the raw CSS `width`/`height` declarations (value + unit) plus source metadata coming out of style resolution.
- **Resolved Dimension**: Holds the final normalized width and height (points), captures the originating unit metadata, and records fallback reasoning (unsupported units, complex height flows) consumed by box/fragment builders.
- **Fragment Dimension**: Carries the renderable rectangle per element, referencing the Resolved Dimension and exposing overflow behavior for downstream renderers and observers.

## Assumptions

- Feature scope targets block-level containers (including bordered placeholders); inline height adjustments remain unsupported beyond existing line-height behavior.
- Supported units are limited to px and pt; percentages and other units (em, rem, vh, vw, physical units) raise warnings and fall back to auto until future phases.
- Height derivations only cover simple block containers that can be resolved in one measurement pass; any layout needing iterative height solving logs the limitation and keeps the element on auto height.
- Overflow handling follows existing engine defaults: content clipped unless overflow rules already implemented for the element type.

## Success Criteria (mandatory)

### Measurable Outcomes

- **SC-001**: For each regression fixture, the resolved fragment width **and** height must each stay within +/- 1 pt of the expected measurement on the supported Windows runner in Release test runs (determinism verified via archived Html2x.TestConsole outputs).
- **SC-002**: Exercise the pixel and invalid-input scenarios sequentially, including a regression that proves percentage units emit warnings and fall back to auto; introduce one failing automated test, implement the minimal passing change, refactor, then move to the next scenario only after the suite is green.
- **SC-003**: Support engineers can identify dimension-related issues within five minutes using the structured diagnostics, proven by a dry run log review.
- **SC-004**: Feature-branch runs of `dotnet test Html2x.sln -c Release` plus the Html2x.TestConsole width/height fixtures complete without manual PDF tweaks, proven by archiving the generated PDFs and logs (as-produced) under `build/width-height/`.
- **SC-005**: Height resolution tests must show the value stabilizing within a single measurement pass; any attempt that would require iterative passes logs the limitation and the test asserts the fallback behavior.



