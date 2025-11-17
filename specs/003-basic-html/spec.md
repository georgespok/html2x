# Feature Specification: Basic HTML-to-PDF Essentials

**Feature Branch**: `003-basic-html`
**Created**: 2025-11-17
**Status**: Draft
**Input**: "add new html to pdf with simple features:1. <br> 2. Basic <img> (explicit width/height, aspect ratio) 3. Simple CSS properties: color, line-height, text-align, background-color, max-width 4. Border enhancements (per side, thickness, color) 5. Default HTML display roles (block, inline, inline-block, list-item) They don't require inline formatting, typography, block formatting contexts, or multi-page logic. Their purpose is to provide essential HTML functionality early, while keeping the system stable and simple."


## Clarifications

### Session 2025-11-17
- Q: Which image sources should the MVP support? → A: File paths plus embedded data URIs; remote fetches stay out of scope.

## User Scenarios & Testing (mandatory)

Each story calls out stage isolation, determinism through Html2x.Diagnostics, and Goal-Driven checkpoints so the plan can reference them directly.

### User Story 1 - Flow Text With Line Breaks (Priority: P1)

Report authors can rely on `<br>` tags and baseline CSS (`color`, `line-height`, `text-align`) to produce predictable inline text flow without adding custom extensions.

**Why this priority**: Text rendering is the minimal viable foundation; most documents contain paragraphs with manual line breaks and simple styling.

**Independent Test**: Layout test feeding HTML with `<p>` and `<br>` tags plus text formatting verifies diagnostics fragments show expected text runs and spacing; integration test uses TestConsole to emit diagnostics JSON for manual review.

**Acceptance Scenarios**:

1. **Given** a paragraph with multiple `<br>` tags, **When** converted, **Then** diagnostics show distinct line fragments in expected order and `line-height` is respected.
2. **Given** inline text using `color` and `text-align`, **When** rendered, **Then** fragment metadata records the requested color and alignment without requiring PDF inspection.

**Reflection Notes – 2025-11-18**

- `InlineFragmentStage` now flushes a new `LineBoxFragment` whenever a `<br>` appears and decorates each line with normalized color, `LineHeight`, and `TextAlign`, which keeps diagnostics deterministic even before rendering.
- `FragmentDiagnosticsWriter` plus the HtmlConverter-level snapshot ensures tests validate line ordering and styling strictly through `Html2x.Diagnostics`, so we no longer need PDF parsing to prove SC-001.
- QuestPDF rendering consumes only the abstractions (`LineBoxFragment.TextAlign`, text colors) while isolation tests guard against LayoutEngine dependencies, keeping stage separation intact as required by Principle VI.

---

### User Story 2 - Embed Basic Images (Priority: P1)

Authors can place basic `<img>` elements with explicit width/height while Html2x preserves aspect ratio and prevents layout overflows by honoring `max-width`.

**Why this priority**: Many MVP documents embed logos or screenshots; enforcing explicit sizing keeps stability without multi-page logic.

**Independent Test**: Layout test supplies inline and block images and asserts diagnostics captured pixel sizes/aspect ratios; integration scenario compares fragment bounding boxes to expected values from options.

**Acceptance Scenarios**:

1. **Given** an `<img>` tag with both width and height, **When** converted, **Then** diagnostics record exact width/height used in layout.
2. **Given** an `<img>` tag lacking one dimension but with an intrinsic ratio, **When** converted, **Then** layout fills the missing dimension from aspect ratio without exceeding `max-width`.

---

### User Story 3 - Apply Borders and Default Display Roles (Priority: P2)

Html2x honors per-side border definitions (color, style, thickness) and respects default HTML display roles (block, inline, inline-block, list-item) to keep predictable layout stacking without advanced formatting contexts.

**Why this priority**: Without default display roles and borders, the MVP output looks incorrect compared to basic browser expectations.

**Independent Test**: Layout tests define elements with different display roles and borders, verifying fragment tree order and border metadata; integration test renders unordered and ordered lists then inspects diagnostics for list item markers.

**Acceptance Scenarios**:

1. **Given** a block element declaring distinct border styles per side, **When** rendered, **Then** diagnostics capture each side's thickness and color individually.
2. **Given** list elements without explicit display overrides, **When** converted, **Then** fragments appear with list-item roles and bullet metadata according to default display mapping.

### Edge Cases

- Missing intrinsic image dimensions: fallback to provided attributes; if both missing, flag via diagnostics and skip rendering to keep stability.
- Text using unsupported CSS props must be ignored without crashing; diagnostics should note the omission.
- Border definitions exceeding logical max (e.g., >20px) must clamp to prevent layout spillover.
- Nested display roles (inline inside inline-block) should defer advanced block formatting contexts but still honor width constraints by inheriting parent role defaults.

## Requirements (mandatory)

### Functional Requirements

- **FR-001**: Implementation MUST respect pipeline contracts; no stage bypassing without new shared abstractions (Principle I).
- **FR-002**: Feature MUST keep fragment semantics predictable for identical inputs and document how `Html2x.Diagnostics` captures deviation signals instead of parsing PDFs (Principle II).
- **FR-003**: Automated tests MUST be authored first and fail before implementation begins (Principle III).
- **FR-004**: `Html2x.Diagnostics` instrumentation MUST cover the new behavior with actionable metadata (Principle IV).
- **FR-005**: Public surface changes MUST include migration guidance and docs updates before release (Principle V).
- **FR-006**: Specifications MUST articulate the Goal-Driven Delivery cadence (state assessment, ordered actions with dependencies, adaptive checkpoints, rollback plans, reflection tasks) for each user story (Principle VI).
- **FR-007**: `<img>` elements without explicit width/height MUST derive one dimension from intrinsic metadata and enforce `max-width` to avoid overflow.
- **FR-008**: `<br>` handling MUST insert fragment breaks without mutating underlying DOM nodes to keep staged layout immutable.
- **FR-009**: Border properties MUST support per-side styling using CSS syntax (`border-top-width`, etc.) and map to fragment metadata for renderers.
- **FR-010**: Default display roles MUST be respected according to HTML specification tables for block, inline, inline-block, and list-item.

### Assumptions

- Image sources are limited to disk file paths and embedded data URIs; remote HTTP(S) fetches remain out of scope.
- All HTML input is trusted and sanitized elsewhere; no need for security filtering in this feature.
- Only RGB color values are required for MVP; advanced color profiles stay out of scope.
- Documents remain single-page for this release; pagination rules beyond single page are deferred.

## Success Criteria (mandatory)

- **SC-001**: Authors can render paragraphs using `<br>` with `color`, `line-height`, and `text-align`, validated by diagnostics snapshots in under 5 minutes per scenario.
- **SC-002**: Images with explicit sizing retain aspect ratio with <2% variance between requested and produced dimensions as verified via diagnostics snapshots in the TestConsole single-page run.
- **SC-003**: Border metadata appears for 100% of elements specifying per-side borders, and diagnostics show the four-side payload within a single console capture.
- **SC-004**: Default display roles ensure list items and inline-block elements appear in correct order with no overlap in at least three regression fixtures.
- **SC-005**: All new behaviors are covered by automated tests that fail when diagnostics drift outside expected ranges, evidenced by CI logs.
- **SC-006**: Running `specs/003-basic-html/samples/basic.html` through the TestConsole maintains combined line-box + PDF rendering time within ±5% of the recorded baseline and introduces no additional renderer allocations (as observed via diagnostics counters).

## Key Entities (include if feature involves data)

- **FragmentTextRun**: Represents inline text with styling such as `color`, `line-height`, and `text-align`; remains unchanged while line breaks are represented by separate `LineBoxFragment` entries.
- **LineBoxFragment**: Holds the runs for a single physical line; the layout engine MUST create a new line box whenever a `<br>` or break-after rule occurs so diagnostics can verify ordering.
- **FragmentImage**: Carries width, height, intrinsic ratio metadata, and references to source assets for renderer consumption.
- **FragmentBorderMetadata**: New or extended structure capturing per-side border thickness, style, and color.
- **DisplayRoleMap**: Table/structure mapping HTML tags to default display roles consumed during layout.









